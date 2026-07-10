namespace BkpUploader.Application.Models;

public sealed record BackupSynchronizationResult(
    int DiscoveredLocalFiles,
    int DiscoveredCloudFiles,
    int UploadedFiles,
    int DeletedLocalFiles,
    int DeletedCloudFiles,
    int SkippedFiles);
