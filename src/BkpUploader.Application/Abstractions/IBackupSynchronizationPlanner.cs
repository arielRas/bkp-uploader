using BkpUploader.Application.Models;

namespace BkpUploader.Application.Abstractions;

public interface IBackupSynchronizationPlanner
{
    BackupSynchronizationPlan BuildPlan(
        BackupDiscoveryResult discoveryResult,
        IReadOnlyCollection<CloudBackupFile> cloudBackups,
        BackupRetentionPolicy retentionPolicy,
        DateTimeOffset utcNow);
}
