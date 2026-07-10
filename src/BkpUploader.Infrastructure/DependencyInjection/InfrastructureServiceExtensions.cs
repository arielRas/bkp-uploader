using BkpUploader.Application.Abstractions;
using BkpUploader.Application.Options;
using BkpUploader.Infrastructure.Common;
using BkpUploader.Infrastructure.FileSystem;
using BkpUploader.Infrastructure.GoogleDrive;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BkpUploader.Infrastructure.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<BackupOptions>(configuration.GetSection(nameof(BackupOptions)));
        services.Configure<BackupRetentionOptions>(configuration.GetSection(nameof(BackupRetentionOptions)));
        services.Configure<GoogleDriveOptions>(configuration.GetSection(nameof(GoogleDriveOptions)));

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IFileSystemService, PhysicalFileSystemService>();
        services.AddSingleton<GoogleDriveServiceFactory>();
        services.AddSingleton<ICloudStorageProvider, GoogleDriveCloudStorageProvider>();

        return services;
    }
}
