namespace BkpUploader.Application.Models;

public sealed record BackupSynchronizationPlan(
    IReadOnlyList<LocalBackupFile> FilesToUpload,
    IReadOnlyList<LocalBackupFile> FilesToDeleteLocally,
    IReadOnlyList<CloudBackupFile> FilesToDeleteInCloud,
    IReadOnlyList<LocalBackupFile> InvalidLocalFiles);
