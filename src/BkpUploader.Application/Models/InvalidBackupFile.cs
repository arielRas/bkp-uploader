namespace BkpUploader.Application.Models;

public sealed record InvalidBackupFile(
    LocalBackupFile LocalFile,
    string Reason);
