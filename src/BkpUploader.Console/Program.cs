using BkpUploader.Application.Abstractions;
using BkpUploader.Application.DependencyInjection;
using BkpUploader.Application.Models;
using BkpUploader.Application.UseCases;
using BkpUploader.Console.DependencyInjection;
using BkpUploader.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Configuration.SetBasePath(AppContext.BaseDirectory);
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);

builder.Services.AddCustomLogging();
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

using IHost host = builder.Build();

IBackupSynchronizationHandler synchronizationHandler = host.Services.GetRequiredService<IBackupSynchronizationHandler>();

try
{
    BackupSynchronizationResult result = await synchronizationHandler.HandleAsync(new SynchronizeBackupsUseCase());

    Log.Warning(
        "Backup synchronization finished. Local discovered: {DiscoveredLocalFiles}. Cloud discovered: {DiscoveredCloudFiles}. Uploaded: {UploadedFiles}. Deleted local: {DeletedLocalFiles}. Deleted cloud: {DeletedCloudFiles}. Skipped: {SkippedFiles}.",
        result.DiscoveredLocalFiles,
        result.DiscoveredCloudFiles,
        result.UploadedFiles,
        result.DeletedLocalFiles,
        result.DeletedCloudFiles,
        result.SkippedFiles);

    return 0;
}
catch (OperationCanceledException)
{
    Log.Warning("Backup synchronization was canceled.");
    return 2;
}
catch (Exception ex)
{
    Log.Warning(ex, "Backup synchronization failed.");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}
