using System.Net;
using TestPr;
using TestPr.DAL;
using TestPr.Service.Settings;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();
        services.AddHttpClient("ConfiguredHttpMessageHandler").ConfigurePrimaryHttpMessageHandler((c) => new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        });
        services.AddHttpClient();
        services.ServiceDependencies();
        services.DALDependencies();
    })
    .Build();

await host.RunAsync();
