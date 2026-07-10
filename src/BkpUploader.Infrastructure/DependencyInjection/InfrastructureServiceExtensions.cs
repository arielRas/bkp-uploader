using BkpUploader.Application.Abstractions;
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
        services.Configure<GoogleDriveOptions>(configuration.GetSection("GoogleDrive"));

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IFileSystemService, PhysicalFileSystemService>();
        services.AddSingleton<GoogleDriveServiceFactory>();
        services.AddSingleton<ICloudStorageProvider, GoogleDriveCloudStorageProvider>();

        return services;
    }
}
