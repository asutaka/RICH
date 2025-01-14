using StockPr;
using StockPr.DAL.Settings;
using StockPr.Service.Settings;

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
