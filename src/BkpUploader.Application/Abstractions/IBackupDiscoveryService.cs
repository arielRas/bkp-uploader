using BkpUploader.Application.Models;

namespace BkpUploader.Application.Abstractions;

public interface IBackupDiscoveryService
{
    Task<BackupDiscoveryResult> DiscoverAsync(CancellationToken cancellationToken = default);
}
