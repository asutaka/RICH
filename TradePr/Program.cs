using TradePr;
using TradePr.DAL.Settings;
using TradePr.Service.Settings;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHttpClient();
        services.AddHostedService<Worker>();
        services.ServiceDependencies();
        services.DALDependencies();
    })
    .UseWindowsService()
    .Build();

await host.RunAsync();
