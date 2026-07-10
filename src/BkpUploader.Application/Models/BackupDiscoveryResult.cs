namespace BkpUploader.Application.Models;

public sealed record BackupDiscoveryResult(
    IReadOnlyList<DiscoveredBackupFile> ValidBackups,
    IReadOnlyList<InvalidBackupFile> InvalidBackups);
