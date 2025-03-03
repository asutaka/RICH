using TradePr;
using TradePr.DAL.Settings;
using TradePr.Service.Settings;
using TradePr.Utils;

IConfiguration configuration = new ConfigurationBuilder()
                            .AddJsonFile("appsettings.json")
                            .Build();
StaticVal._binance_key = configuration["Binance:API_KEY"];
StaticVal._binance_secret = configuration["Binance:SECRET_KEY"];
StaticVal._bybit_key = configuration["ByBit:API_KEY"];
StaticVal._bybit_secret = configuration["ByBit:SECRET_KEY"];

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHttpClient();
        services.AddMemoryCache();
        services.AddHostedService<Worker>();
        services.ServiceDependencies();
        services.DALDependencies();
        //services.AddBinance(options => {
        //    // Configure options in code
        //    options.ApiCredentials = new ApiCredentials(configuration["Account:API_KEY"], configuration["Account:SECRET_KEY"]);
        //});
    })
    .UseWindowsService()
    .Build();

await host.RunAsync();
