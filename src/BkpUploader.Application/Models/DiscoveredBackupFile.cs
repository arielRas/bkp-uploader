namespace BkpUploader.Application.Models;

public sealed record DiscoveredBackupFile(
    LocalBackupFile LocalFile,
    ParsedBackupName ParsedName);
