using BkpUploader.Application.Models;

namespace BkpUploader.Application.Abstractions;

public interface ICloudStorageProvider
{
    Task<IReadOnlyList<CloudBackupFile>> ListBackupsAsync(CancellationToken cancellationToken = default);

    Task<CloudBackupFile> UploadAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(string fileId, CancellationToken cancellationToken = default);
}
