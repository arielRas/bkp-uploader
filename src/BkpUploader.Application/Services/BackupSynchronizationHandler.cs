using BkpUploader.Application.Abstractions;
using BkpUploader.Application.Models;
using BkpUploader.Application.Options;
using BkpUploader.Application.UseCases;
using Microsoft.Extensions.Options;

namespace BkpUploader.Application.Services;

public sealed class BackupSynchronizationHandler : IBackupSynchronizationHandler
{
    private readonly IBackupDiscoveryService _backupDiscoveryService;
    private readonly IBackupSynchronizationPlanner _planner;
    private readonly ICloudStorageProvider _cloudStorageProvider;
    private readonly IFileSystemService _fileSystemService;
    private readonly IClock _clock;
    private readonly BackupRetentionOptions _retentionOptions;

    public BackupSynchronizationHandler(
        IBackupDiscoveryService backupDiscoveryService,
        IBackupSynchronizationPlanner planner,
        ICloudStorageProvider cloudStorageProvider,
        IFileSystemService fileSystemService,
        IClock clock,
        IOptions<BackupRetentionOptions> retentionOptions)
    {
        _backupDiscoveryService = backupDiscoveryService;
        _planner = planner;
        _cloudStorageProvider = cloudStorageProvider;
        _fileSystemService = fileSystemService;
        _clock = clock;
        _retentionOptions = retentionOptions.Value;
    }

    public async Task<BackupSynchronizationResult> HandleAsync(
        SynchronizeBackupsUseCase _request,
        CancellationToken cancellationToken = default)
    {
        BackupDiscoveryResult discoveryResult = await _backupDiscoveryService.DiscoverAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<CloudBackupFile> cloudBackups = await _cloudStorageProvider.ListBackupsAsync(cancellationToken).ConfigureAwait(false);
        BackupRetentionPolicy retentionPolicy = BackupRetentionPolicy.FromOptions(_retentionOptions);

        BackupSynchronizationPlan plan = _planner.BuildPlan(
            discoveryResult,
            cloudBackups,
            retentionPolicy,
            _clock.UtcNow);

        int uploadedFiles = 0;
        int deletedLocalFiles = 0;
        int deletedCloudFiles = 0;

        foreach (DiscoveredBackupFile fileToUpload in plan.FilesToUpload)
        {
            await using Stream content = await _fileSystemService.OpenReadAsync(fileToUpload.LocalFile.FullPath, cancellationToken).ConfigureAwait(false);
            await _cloudStorageProvider.UploadAsync(content, fileToUpload.LocalFile.FileName, cancellationToken).ConfigureAwait(false);
            uploadedFiles++;
        }

        foreach (DiscoveredBackupFile fileToDelete in plan.FilesToDeleteLocally)
        {
            if (await _fileSystemService.ExistsAsync(fileToDelete.LocalFile.FullPath, cancellationToken).ConfigureAwait(false))
            {
                await _fileSystemService.DeleteAsync(fileToDelete.LocalFile.FullPath, cancellationToken).ConfigureAwait(false);
                deletedLocalFiles++;
            }
        }

        foreach (CloudBackupFile cloudFileToDelete in plan.FilesToDeleteInCloud)
        {
            await _cloudStorageProvider.DeleteAsync(cloudFileToDelete.Id, cancellationToken).ConfigureAwait(false);
            deletedCloudFiles++;
        }

        int skippedFiles = plan.InvalidLocalFiles.Count + plan.FilesAlreadyPresentInCloud.Count;

        return new BackupSynchronizationResult(
            discoveryResult.ValidBackups.Count + discoveryResult.InvalidBackups.Count,
            cloudBackups.Count,
            uploadedFiles,
            deletedLocalFiles,
            deletedCloudFiles,
            skippedFiles);
    }
}
