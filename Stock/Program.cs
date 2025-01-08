using Stock;
using Stock.Service.Settings;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();
        services.ServiceDependencies();
    })
    .Build();

await host.RunAsync();
