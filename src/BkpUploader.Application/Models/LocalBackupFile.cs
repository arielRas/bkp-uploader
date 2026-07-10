namespace BkpUploader.Application.Models;

public sealed record LocalBackupFile(
    string FullPath,
    string FileName,
    long SizeInBytes,
    DateTimeOffset LastModifiedUtc);
