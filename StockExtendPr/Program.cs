using StockExtendPr;
using StockExtendPr.DAL.Settings;
using StockExtendPr.Service.Settings;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();
        services.AddHttpClient();
        services.ServiceDependencies();
        services.DALDependencies();
    })
    .UseWindowsService()
    .Build();

await host.RunAsync();
