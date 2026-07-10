using BkpUploader.Application.Models;

namespace BkpUploader.Application.Abstractions;

public interface IBackupSynchronizationService
{
    Task<BackupSynchronizationResult> SynchronizeAsync(CancellationToken cancellationToken = default);
}
