using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

IHostBuilder builder = Host.CreateDefaultBuilder();

builder.ConfigureServices((context, services) =>
{
    IConfiguration configuration = context.Configuration;

    //services.MetodoExtensionPersonalizado()
    //        .MetodoExtensionPersonalizado();
});

builder.ConfigureAppConfiguration((context, config) =>
{
    config.SetBasePath(AppContext.BaseDirectory);
    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
    config.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);
});

IHost AppHost = builder.Build();