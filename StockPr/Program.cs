using StockPr;
using StockPr.DAL.Settings;
using StockPr.Service.Settings;
using StockPr.Settings;
using StockPr.Utils;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Configure settings from appsettings.json
        services.Configure<TelegramSettings>(
            context.Configuration.GetSection("Telegram"));
        services.Configure<VietStockSettings>(
            context.Configuration.GetSection("VietStock"));
        services.Configure<MongoDbSettings>(
            context.Configuration.GetSection("MongoDB"));
        
        services.AddHostedService<Worker>();
        services.AddHttpClient();
        services.ServiceDependencies();
        services.DALDependencies(context.Configuration);
        
        // Initialize StaticVal with VietStock credentials from configuration
        var vietStockSettings = context.Configuration.GetSection("VietStock").Get<VietStockSettings>();
        if (vietStockSettings != null)
        {
            StaticVal._VietStock_Cookie = vietStockSettings.Cookie;
            StaticVal._VietStock_Token = vietStockSettings.Token;
        }
    })
    .UseWindowsService()
    .Build();

await host.RunAsync();
