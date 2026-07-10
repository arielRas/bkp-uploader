using BkpUploader.Application.Abstractions;
using BkpUploader.Application.Models;

namespace BkpUploader.Application.Services;

public sealed class BackupSynchronizationPlanner : IBackupSynchronizationPlanner
{
    private readonly IBackupRetentionService _retentionService;
    private readonly IBackupNameParser _backupNameParser;

    public BackupSynchronizationPlanner(
        IBackupRetentionService retentionService,
        IBackupNameParser backupNameParser)
    {
        _retentionService = retentionService;
        _backupNameParser = backupNameParser;
    }

    public BackupSynchronizationPlan BuildPlan(
        BackupDiscoveryResult discoveryResult,
        IReadOnlyCollection<CloudBackupFile> cloudBackups,
        BackupRetentionPolicy retentionPolicy,
        DateTimeOffset utcNow)
    {
        Dictionary<string, List<CloudBackupFile>> cloudFilesByName = cloudBackups
            .GroupBy(file => file.FileName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

        List<DiscoveredBackupFile> filesToUpload = new();
        List<DiscoveredBackupFile> filesToDeleteLocally = new();
        List<CloudBackupFile> filesToDeleteInCloud = new();
        List<DiscoveredBackupFile> filesAlreadyPresentInCloud = new();

        HashSet<string> cloudDeletionIds = new(StringComparer.OrdinalIgnoreCase);

        foreach (DiscoveredBackupFile localBackup in discoveryResult.ValidBackups)
        {
            if (_retentionService.IsExpired(localBackup.ParsedName, retentionPolicy, utcNow))
            {
                filesToDeleteLocally.Add(localBackup);
                continue;
            }

            if (cloudFilesByName.TryGetValue(localBackup.LocalFile.FileName, out List<CloudBackupFile>? cloudMatches))
            {
                bool hasExactMatch = cloudMatches.Any(cloudFile => cloudFile.SizeInBytes == localBackup.LocalFile.SizeInBytes);
                if (hasExactMatch)
                {
                    filesAlreadyPresentInCloud.Add(localBackup);
                    continue;
                }

                foreach (CloudBackupFile cloudMatch in cloudMatches)
                {
                    if (cloudDeletionIds.Add(cloudMatch.Id))
                    {
                        filesToDeleteInCloud.Add(cloudMatch);
                    }
                }
            }

            filesToUpload.Add(localBackup);
        }

        foreach (CloudBackupFile cloudBackup in cloudBackups)
        {
            if (!_backupNameParser.TryParse(cloudBackup.FileName, out ParsedBackupName? parsedName) || parsedName is null)
            {
                continue;
            }

            if (_retentionService.IsExpired(parsedName, retentionPolicy, utcNow) && cloudDeletionIds.Add(cloudBackup.Id))
            {
                filesToDeleteInCloud.Add(cloudBackup);
            }
        }

        return new BackupSynchronizationPlan(
            filesToUpload,
            filesToDeleteLocally,
            filesToDeleteInCloud,
            discoveryResult.InvalidBackups,
            filesAlreadyPresentInCloud);
    }
}
