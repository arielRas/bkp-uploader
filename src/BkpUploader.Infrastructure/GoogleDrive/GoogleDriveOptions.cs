namespace BkpUploader.Infrastructure.GoogleDrive;

public sealed class GoogleDriveOptions
{
    public string ApplicationName { get; set; } = "BkpUploader";

    public string? CredentialFilePath { get; set; }

    public string? RootFolderId { get; set; }

    public string FullFolderName { get; set; } = "Full";

    public string DifferentialFolderName { get; set; } = "Differential";

    public string TransactionLogFolderName { get; set; } = "TransactionLog";
}
