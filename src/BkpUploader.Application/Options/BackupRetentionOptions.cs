namespace BkpUploader.Application.Options;

public sealed class BackupRetentionOptions
{
    public int FullDays { get; set; } = 5;

    public int DifferentialDays { get; set; } = 3;

    public int TransactionLogDays { get; set; } = 3;
}
