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
    private readonly ILogger<GoogleDriveCloudStorageProvider> _logger;

    public GoogleDriveCloudStorageProvider(
        GoogleDriveServiceFactory serviceFactory,
        IOptions<GoogleDriveOptions> options,
        ILogger<GoogleDriveCloudStorageProvider> logger)
    {
        _driveService = serviceFactory.Create();
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CloudBackupFile>> ListBackupsAsync(CancellationToken cancellationToken = default)
    {
        FilesResource.ListRequest request = _driveService.Files.List();
        request.Fields = "files(id, name, size, modifiedTime)";
        request.PageSize = 1000;
        request.Q = string.IsNullOrWhiteSpace(_options.RootFolderId)
            ? "trashed = false"
            : $"'{_options.RootFolderId}' in parents and trashed = false";

        Google.Apis.Drive.v3.Data.FileList response = await request.ExecuteAsync(cancellationToken).ConfigureAwait(false);

        List<CloudBackupFile> backups = new();

        if (response.Files is null)
        {
            return backups;
        }

        foreach (GoogleDriveFile file in response.Files)
        {
            backups.Add(new CloudBackupFile(
                file.Id ?? string.Empty,
                file.Name ?? string.Empty,
                file.Size ?? 0,
                file.ModifiedTimeDateTimeOffset ?? DateTimeOffset.UnixEpoch));
        }

        return backups;
    }

    public async Task<CloudBackupFile> UploadAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        GoogleDriveFile metadata = new()
        {
            Name = fileName
        };

        if (!string.IsNullOrWhiteSpace(_options.RootFolderId))
        {
            metadata.Parents = new List<string> { _options.RootFolderId };
        }

        FilesResource.CreateMediaUpload request = _driveService.Files.Create(metadata, content, DefaultMimeType);
        request.Fields = "id, name, size, modifiedTime";

        await request.UploadAsync(cancellationToken).ConfigureAwait(false);

        GoogleDriveFile response = request.ResponseBody ?? throw new InvalidOperationException("Google Drive upload did not return a file.");

        _logger.LogInformation("Uploaded file {FileName} to Google Drive.", fileName);

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

        _logger.LogInformation("Deleted file {FileId} from Google Drive.", fileId);
    }
}
