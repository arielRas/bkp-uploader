namespace BkpUploader.Application.Options;

public sealed class BackupOptions
{
    public string FullBackupRootPath { get; set; } = string.Empty;

    public string DifferentialBackupRootPath { get; set; } = string.Empty;

    public string TransactionLogBackupRootPath { get; set; } = string.Empty;
}
