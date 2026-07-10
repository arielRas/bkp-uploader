using BkpUploader.Application.Options;

namespace BkpUploader.Application.Models;

public sealed record BackupRetentionPolicy(TimeSpan FullRetention, TimeSpan DiffRetention, TimeSpan TransactionLogRetention)
{
    public static BackupRetentionPolicy FromOptions(BackupRetentionOptions options)
    {
        return new BackupRetentionPolicy(
            TimeSpan.FromDays(options.FullDays),
            TimeSpan.FromDays(options.DifferentialDays),
            TimeSpan.FromDays(options.TransactionLogDays));
    }

    public TimeSpan GetRetentionFor(BackupType backupType)
    {
        return backupType switch
        {
            BackupType.Full => FullRetention,
            BackupType.Diff => DiffRetention,
            BackupType.TransactionLog => TransactionLogRetention,
            _ => throw new ArgumentOutOfRangeException(nameof(backupType), backupType, null)
        };
    }
}
