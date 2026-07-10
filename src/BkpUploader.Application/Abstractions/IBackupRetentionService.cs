using BkpUploader.Application.Models;

namespace BkpUploader.Application.Abstractions;

public interface IBackupRetentionService
{
    TimeSpan GetRetentionWindow(BackupType backupType, BackupRetentionPolicy policy);

    bool IsExpired(ParsedBackupName backupName, BackupRetentionPolicy policy, DateTimeOffset utcNow);
}
