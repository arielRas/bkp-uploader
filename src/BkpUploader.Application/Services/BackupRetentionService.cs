using BkpUploader.Application.Abstractions;
using BkpUploader.Application.Models;

namespace BkpUploader.Application.Services;

public sealed class BackupRetentionService : IBackupRetentionService
{
    public TimeSpan GetRetentionWindow(BackupType backupType, BackupRetentionPolicy policy)
    {
        return policy.GetRetentionFor(backupType);
    }

    public bool IsExpired(ParsedBackupName backupName, BackupRetentionPolicy policy, DateTimeOffset utcNow)
    {
        TimeSpan retentionWindow = GetRetentionWindow(backupName.BackupType, policy);
        DateTimeOffset expirationThreshold = backupName.BackupTimestampUtc.Add(retentionWindow);
        return utcNow >= expirationThreshold;
    }
}
