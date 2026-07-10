namespace BkpUploader.Application.Models;

public sealed record BackupRetentionPolicy(TimeSpan FullRetention, TimeSpan DiffRetention, TimeSpan TransactionLogRetention)
{
    public static BackupRetentionPolicy Default { get; } = new(
        TimeSpan.FromDays(30),
        TimeSpan.FromDays(7),
        TimeSpan.FromDays(3));

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
