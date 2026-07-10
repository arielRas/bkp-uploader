using BkpUploader.Application.Models;

namespace BkpUploader.Application.Abstractions;

public interface IBackupSynchronizationPlanner
{
    BackupSynchronizationPlan BuildPlan(
        IReadOnlyCollection<LocalBackupFile> localBackups,
        IReadOnlyCollection<CloudBackupFile> cloudBackups,
        BackupRetentionPolicy retentionPolicy,
        DateTimeOffset utcNow);
}
