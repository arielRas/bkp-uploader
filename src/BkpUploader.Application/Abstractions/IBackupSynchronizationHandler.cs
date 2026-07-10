using BkpUploader.Application.Models;
using BkpUploader.Application.UseCases;

namespace BkpUploader.Application.Abstractions;

public interface IBackupSynchronizationHandler
{
    Task<BackupSynchronizationResult> HandleAsync(
        SynchronizeBackupsUseCase request,
        CancellationToken cancellationToken = default);
}
