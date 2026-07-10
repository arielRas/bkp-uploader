namespace BkpUploader.Infrastructure.GoogleDrive;

public sealed class GoogleDriveOptions
{
    public string ApplicationName { get; set; } = "BkpUploader";

    public string? CredentialFilePath { get; set; }

    public string? FolderId { get; set; }
}
