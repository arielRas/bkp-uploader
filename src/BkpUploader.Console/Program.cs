using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

Console.WriteLine("Hello, World!");

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
});

IHost AppHost = builder.Build();