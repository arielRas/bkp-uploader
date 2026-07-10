namespace BkpUploader.Application.Models;

public sealed record BackupSynchronizationPlan(
    IReadOnlyList<DiscoveredBackupFile> FilesToUpload,
    IReadOnlyList<DiscoveredBackupFile> FilesToDeleteLocally,
    IReadOnlyList<CloudBackupFile> FilesToDeleteInCloud,
    IReadOnlyList<InvalidBackupFile> InvalidLocalFiles,
    IReadOnlyList<DiscoveredBackupFile> FilesAlreadyPresentInCloud);
