using BkpUploader.Application.Models;

namespace BkpUploader.Application.Abstractions;

public interface IBackupDiscoveryService
{
    Task<IReadOnlyList<LocalBackupFile>> DiscoverAsync(CancellationToken cancellationToken = default);
}
