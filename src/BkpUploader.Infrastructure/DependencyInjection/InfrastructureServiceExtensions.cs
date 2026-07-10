using BkpUploader.Application.Abstractions;
using BkpUploader.Application.Options;
using BkpUploader.Infrastructure.Common;
using BkpUploader.Infrastructure.CloudflareR2;
using BkpUploader.Infrastructure.FileSystem;
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
        services.Configure<CloudflareR2Options>(configuration.GetSection(nameof(CloudflareR2Options)));

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IFileSystemService, PhysicalFileSystemService>();
        services.AddSingleton<CloudflareR2ClientFactory>();
        services.AddSingleton<ICloudStorageProvider, CloudflareR2CloudStorageProvider>();

        return services;
    }
}
