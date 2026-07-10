using BkpUploader.Application.Abstractions;
using BkpUploader.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BkpUploader.Application.DependencyInjection;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<IBackupNameParser, BackupNameParser>();
        services.AddSingleton<IBackupRetentionService, BackupRetentionService>();
        services.AddSingleton<IBackupDiscoveryService, BackupDiscoveryService>();
        services.AddSingleton<IBackupSynchronizationPlanner, BackupSynchronizationPlanner>();
        services.AddSingleton<IBackupSynchronizationHandler, BackupSynchronizationHandler>();

        return services;
    }
}
