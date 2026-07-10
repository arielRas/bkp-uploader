using Amazon.S3;
using Amazon.S3.Model;
using BkpUploader.Application.Abstractions;
using BkpUploader.Application.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BkpUploader.Infrastructure.CloudflareR2;

public sealed class CloudflareR2CloudStorageProvider : ICloudStorageProvider
{
    private const string DefaultContentType = "application/octet-stream";

    private readonly IAmazonS3 _s3Client;
    private readonly CloudflareR2Options _options;
    private readonly IBackupNameParser _backupNameParser;
    private readonly ILogger<CloudflareR2CloudStorageProvider> _logger;

    public CloudflareR2CloudStorageProvider(
        CloudflareR2ClientFactory clientFactory,
        IBackupNameParser backupNameParser,
        IOptions<CloudflareR2Options> options,
        ILogger<CloudflareR2CloudStorageProvider> logger)
    {
        _s3Client = clientFactory.Create();
        _backupNameParser = backupNameParser;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CloudBackupFile>> ListBackupsAsync(CancellationToken cancellationToken = default)
    {
        List<CloudBackupFile> backups = new();
        string? prefix = BuildRootPrefix();
        string? continuationToken = null;

        do
        {
            ListObjectsV2Request request = new()
            {
                BucketName = RequireBucketName(),
                Prefix = prefix,
                ContinuationToken = continuationToken
            };

            ListObjectsV2Response response;
            try
            {
                response = await _s3Client.ListObjectsV2Async(request, cancellationToken).ConfigureAwait(false);
            }
            catch (AmazonS3Exception ex) when (IsAccessDenied(ex))
            {
                throw new InvalidOperationException(
                    $"Cloudflare R2 denied the ListObjectsV2 call for bucket '{_options.BucketName}'. " +
                    $"Check that ServiceUrl points to the exact R2 S3 endpoint, the bucket name is correct, and the credential has read/list access to this bucket.",
                    ex);
            }

            foreach (S3Object s3Object in response.S3Objects)
            {
                if (string.IsNullOrWhiteSpace(s3Object.Key))
                {
                    continue;
                }

                backups.Add(new CloudBackupFile(
                    s3Object.Key,
                    Path.GetFileName(s3Object.Key),
                    s3Object.Size ?? 0,
                    new DateTimeOffset((s3Object.LastModified ?? DateTime.UtcNow).ToUniversalTime())));
            }

            continuationToken = response.NextContinuationToken;
            if (response.IsTruncated != true)
            {
                break;
            }
        }
        while (!string.IsNullOrWhiteSpace(continuationToken));

        return backups;
    }

    public async Task<CloudBackupFile> UploadAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        if (!_backupNameParser.TryParse(fileName, out ParsedBackupName? parsedName) || parsedName is null)
        {
            throw new InvalidOperationException($"Unable to determine backup type from file name '{fileName}'.");
        }

        string objectKey = BuildObjectKey(parsedName.BackupType, fileName);

        if (content.CanSeek)
        {
            content.Position = 0;
        }

        PutObjectRequest request = new()
        {
            BucketName = RequireBucketName(),
            Key = objectKey,
            InputStream = content,
            ContentType = DefaultContentType,
            DisablePayloadSigning = true,
            DisableDefaultChecksumValidation = true
        };

        await _s3Client.PutObjectAsync(request, cancellationToken).ConfigureAwait(false);

        _logger.LogWarning("Uploaded file {FileName} to Cloudflare R2 bucket {BucketName}.", fileName, _options.BucketName);

        return new CloudBackupFile(
            objectKey,
            fileName,
            content.CanSeek ? content.Length : 0,
            DateTimeOffset.UtcNow);
    }

    public async Task DeleteAsync(string fileId, CancellationToken cancellationToken = default)
    {
        DeleteObjectRequest request = new()
        {
            BucketName = RequireBucketName(),
            Key = fileId
        };

        await _s3Client.DeleteObjectAsync(request, cancellationToken).ConfigureAwait(false);

        _logger.LogWarning("Deleted file {FileId} from Cloudflare R2 bucket {BucketName}.", fileId, _options.BucketName);
    }

    private string BuildObjectKey(BackupType backupType, string fileName)
    {
        List<string> segments = new();

        AddSegments(segments, _options.RootPrefix);
        AddSegments(segments, GetBackupPrefix(backupType));
        segments.Add(fileName);

        return string.Join("/", segments);
    }

    private string? BuildRootPrefix()
    {
        List<string> segments = new();
        AddSegments(segments, _options.RootPrefix);

        return segments.Count == 0 ? null : string.Join("/", segments) + "/";
    }

    private static void AddSegments(List<string> segments, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        segments.AddRange(
            value.Replace('\\', '/')
                .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private string GetBackupPrefix(BackupType backupType)
    {
        return backupType switch
        {
            BackupType.Full => EnsureConfigured(_options.FullPrefix, nameof(CloudflareR2Options.FullPrefix)),
            BackupType.Diff => EnsureConfigured(_options.DifferentialPrefix, nameof(CloudflareR2Options.DifferentialPrefix)),
            BackupType.TransactionLog => EnsureConfigured(_options.TransactionLogPrefix, nameof(CloudflareR2Options.TransactionLogPrefix)),
            _ => throw new InvalidOperationException($"Unsupported backup type '{backupType}'.")
        };
    }

    private string RequireBucketName()
    {
        return EnsureConfigured(_options.BucketName, nameof(CloudflareR2Options.BucketName));
    }

    private static string EnsureConfigured(string value, string optionName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{optionName} is required.");
        }

        return value;
    }

    private static bool IsAccessDenied(AmazonS3Exception exception)
    {
        return exception.StatusCode == System.Net.HttpStatusCode.Forbidden ||
               string.Equals(exception.ErrorCode, "AccessDenied", StringComparison.OrdinalIgnoreCase);
    }
}
