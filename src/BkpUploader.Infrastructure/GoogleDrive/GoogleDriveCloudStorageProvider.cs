using BkpUploader.Application.Abstractions;
using BkpUploader.Application.Models;
using Google.Apis.Drive.v3;
using GoogleDriveFile = Google.Apis.Drive.v3.Data.File;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BkpUploader.Infrastructure.GoogleDrive;

public sealed class GoogleDriveCloudStorageProvider : ICloudStorageProvider
{
    private const string DefaultMimeType = "application/octet-stream";

    private readonly DriveService _driveService;
    private readonly GoogleDriveOptions _options;
    private readonly IBackupNameParser _backupNameParser;
    private readonly ILogger<GoogleDriveCloudStorageProvider> _logger;

    public GoogleDriveCloudStorageProvider(
        GoogleDriveServiceFactory serviceFactory,
        IBackupNameParser backupNameParser,
        IOptions<GoogleDriveOptions> options,
        ILogger<GoogleDriveCloudStorageProvider> logger)
    {
        _driveService = serviceFactory.Create();
        _backupNameParser = backupNameParser;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CloudBackupFile>> ListBackupsAsync(CancellationToken cancellationToken = default)
    {
        List<CloudBackupFile> backups = new();

        IReadOnlyDictionary<BackupType, string> folderIds = await ResolveBackupFolderIdsAsync(cancellationToken).ConfigureAwait(false);

        foreach (string folderId in folderIds.Values)
        {
            backups.AddRange(await ListFilesInFolderAsync(folderId, cancellationToken).ConfigureAwait(false));
        }

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

        IReadOnlyDictionary<BackupType, string> folderIds = await ResolveBackupFolderIdsAsync(cancellationToken).ConfigureAwait(false);
        string targetFolderId = folderIds[parsedName.BackupType];

        GoogleDriveFile metadata = new()
        {
            Name = fileName
        };

        metadata.Parents = new List<string> { targetFolderId };

        FilesResource.CreateMediaUpload request = _driveService.Files.Create(metadata, content, DefaultMimeType);
        request.Fields = "id, name, size, modifiedTime";

        await request.UploadAsync(cancellationToken).ConfigureAwait(false);

        GoogleDriveFile response = request.ResponseBody ?? throw new InvalidOperationException("Google Drive upload did not return a file.");

        _logger.LogWarning("Uploaded file {FileName} to Google Drive.", fileName);

        return new CloudBackupFile(
            response.Id ?? string.Empty,
            response.Name ?? fileName,
            response.Size ?? 0,
            response.ModifiedTimeDateTimeOffset ?? DateTimeOffset.UtcNow);
    }

    public async Task DeleteAsync(string fileId, CancellationToken cancellationToken = default)
    {
        FilesResource.DeleteRequest request = _driveService.Files.Delete(fileId);
        await request.ExecuteAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogWarning("Deleted file {FileId} from Google Drive.", fileId);
    }

    private async Task<IReadOnlyList<CloudBackupFile>> ListFilesInFolderAsync(
        string folderId,
        CancellationToken cancellationToken)
    {
        List<CloudBackupFile> backups = new();
        string? pageToken = null;

        do
        {
            FilesResource.ListRequest request = _driveService.Files.List();
            request.Fields = "nextPageToken, files(id, name, size, modifiedTime)";
            request.PageSize = 1000;
            request.Q = $"'{folderId}' in parents and trashed = false";
            request.PageToken = pageToken;

            Google.Apis.Drive.v3.Data.FileList response = await request.ExecuteAsync(cancellationToken).ConfigureAwait(false);

            if (response.Files is not null)
            {
                foreach (GoogleDriveFile file in response.Files)
                {
                    backups.Add(new CloudBackupFile(
                        file.Id ?? string.Empty,
                        file.Name ?? string.Empty,
                        file.Size ?? 0,
                        file.ModifiedTimeDateTimeOffset ?? DateTimeOffset.UnixEpoch));
                }
            }

            pageToken = response.NextPageToken;
        }
        while (!string.IsNullOrWhiteSpace(pageToken));

        return backups;
    }

    private async Task<IReadOnlyDictionary<BackupType, string>> ResolveBackupFolderIdsAsync(
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.RootFolderId))
        {
            throw new InvalidOperationException("GoogleDriveOptions.RootFolderId is required.");
        }

        FilesResource.ListRequest request = _driveService.Files.List();
        request.Fields = "files(id, name)";
        request.PageSize = 1000;
        request.Q = $"'{_options.RootFolderId}' in parents and mimeType = 'application/vnd.google-apps.folder' and trashed = false";

        Google.Apis.Drive.v3.Data.FileList response = await request.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        Dictionary<string, string> folderIdsByName = response.Files is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : response.Files
                .Where(file => !string.IsNullOrWhiteSpace(file.Name) && !string.IsNullOrWhiteSpace(file.Id))
                .ToDictionary(file => file.Name!, file => file.Id!, StringComparer.OrdinalIgnoreCase);

        return new Dictionary<BackupType, string>
        {
            [BackupType.Full] = GetFolderId(folderIdsByName, _options.FullFolderName),
            [BackupType.Diff] = GetFolderId(folderIdsByName, _options.DifferentialFolderName),
            [BackupType.TransactionLog] = GetFolderId(folderIdsByName, _options.TransactionLogFolderName)
        };
    }

    private static string GetFolderId(
        IReadOnlyDictionary<string, string> folderIdsByName,
        string folderName)
    {
        if (string.IsNullOrWhiteSpace(folderName))
        {
            throw new InvalidOperationException("Google Drive folder names cannot be empty.");
        }

        if (!folderIdsByName.TryGetValue(folderName, out string? folderId))
        {
            throw new InvalidOperationException($"Google Drive folder '{folderName}' was not found under the configured root folder.");
        }

        return folderId;
    }
}
