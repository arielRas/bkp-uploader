using BkpUploader.Application.Abstractions;
using BkpUploader.Application.Models;
using BkpUploader.Application.Options;
using Microsoft.Extensions.Options;

namespace BkpUploader.Application.Services;

public sealed class BackupDiscoveryService : IBackupDiscoveryService
{
    private readonly IFileSystemService _fileSystemService;
    private readonly IBackupNameParser _backupNameParser;
    private readonly BackupOptions _options;

    public BackupDiscoveryService(
        IFileSystemService fileSystemService,
        IBackupNameParser backupNameParser,
        IOptions<BackupOptions> options)
    {
        _fileSystemService = fileSystemService;
        _backupNameParser = backupNameParser;
        _options = options.Value;
    }

    public async Task<BackupDiscoveryResult> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        List<DiscoveredBackupFile> validBackups = new();
        List<InvalidBackupFile> invalidBackups = new();

        await DiscoverFromRootAsync(
            _options.FullBackupRootPath,
            BackupType.Full,
            validBackups,
            invalidBackups,
            cancellationToken).ConfigureAwait(false);

        await DiscoverFromRootAsync(
            _options.DifferentialBackupRootPath,
            BackupType.Diff,
            validBackups,
            invalidBackups,
            cancellationToken).ConfigureAwait(false);

        await DiscoverFromRootAsync(
            _options.TransactionLogBackupRootPath,
            BackupType.TransactionLog,
            validBackups,
            invalidBackups,
            cancellationToken).ConfigureAwait(false);

        return new BackupDiscoveryResult(validBackups, invalidBackups);
    }

    private async Task DiscoverFromRootAsync(
        string rootPath,
        BackupType expectedBackupType,
        ICollection<DiscoveredBackupFile> validBackups,
        ICollection<InvalidBackupFile> invalidBackups,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            return;
        }

        await foreach (string filePath in _fileSystemService.EnumerateFilesAsync(rootPath, cancellationToken).ConfigureAwait(false))
        {
            string fileName = Path.GetFileName(filePath);
            LocalBackupFile localBackupFile = new(
                filePath,
                fileName,
                await _fileSystemService.GetLengthAsync(filePath, cancellationToken).ConfigureAwait(false),
                await _fileSystemService.GetLastWriteTimeUtcAsync(filePath, cancellationToken).ConfigureAwait(false));

            if (!_backupNameParser.TryParse(fileName, out ParsedBackupName? parsedName) || parsedName is null)
            {
                invalidBackups.Add(new InvalidBackupFile(localBackupFile, "File name does not match the expected backup pattern."));
                continue;
            }

            if (parsedName.BackupType != expectedBackupType)
            {
                invalidBackups.Add(new InvalidBackupFile(
                    localBackupFile,
                    $"Expected {expectedBackupType} backup in this folder, but found {parsedName.BackupType}."));
                continue;
            }

            validBackups.Add(new DiscoveredBackupFile(localBackupFile, parsedName));
        }
    }
}
