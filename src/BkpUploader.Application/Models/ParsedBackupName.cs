namespace BkpUploader.Application.Models;

public sealed record ParsedBackupName(string DatabaseName, BackupType BackupType, DateTimeOffset BackupTimestampUtc);
