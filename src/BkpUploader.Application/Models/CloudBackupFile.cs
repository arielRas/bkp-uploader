namespace BkpUploader.Application.Models;

public sealed record CloudBackupFile(
    string Id,
    string FileName,
    long SizeInBytes,
    DateTimeOffset LastModifiedUtc);
