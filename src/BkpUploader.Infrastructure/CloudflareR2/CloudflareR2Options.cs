namespace BkpUploader.Infrastructure.CloudflareR2;

public sealed class CloudflareR2Options
{
    public string ServiceUrl { get; set; } = string.Empty;

    public string AccountId { get; set; } = string.Empty;

    public string AccessKeyId { get; set; } = string.Empty;

    public string SecretAccessKey { get; set; } = string.Empty;

    public string BucketName { get; set; } = string.Empty;

    public string RootPrefix { get; set; } = string.Empty;

    public string FullPrefix { get; set; } = "Full";

    public string DifferentialPrefix { get; set; } = "Differential";

    public string TransactionLogPrefix { get; set; } = "TransactionLog";
}
