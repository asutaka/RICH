using CoinPr;
using CoinPr.DAL.Settings;
using CoinPr.Service.Settings;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();
        services.AddHttpClient();
        services.AddMemoryCache();
        services.ServiceDependencies();
        services.DALDependencies();
    })
    .UseWindowsService()
    .Build();

await host.RunAsync();
