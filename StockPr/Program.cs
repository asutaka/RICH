using StockPr.Config;
using StockPr;
using StockPr.DAL.Settings;
using StockPr.Service.Settings;
using StockPr.Settings;
using StockPr.Utils;
using StockPr.Service;
using System.Net.Http;
using Serilog;

IHost host = Host.CreateDefaultBuilder(args)
    .UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext())
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
        services.AddHttpClient("ResilientClient")
            .AddPolicyHandler((services, request) => 
                HttpPolicies.GetRetryPolicy(services.GetRequiredService<ILogger<HttpClient>>()))
            .AddPolicyHandler((services, request) => 
                HttpPolicies.GetCircuitBreakerPolicy(services.GetRequiredService<ILogger<HttpClient>>()));

        services.AddHttpClient("VietstockClient")
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { UseCookies = false })
            .AddPolicyHandler((services, request) => 
                HttpPolicies.GetRetryPolicy(services.GetRequiredService<ILogger<HttpClient>>()))
            .AddPolicyHandler((services, request) => 
                HttpPolicies.GetCircuitBreakerPolicy(services.GetRequiredService<ILogger<HttpClient>>()));

        services.AddHttpClient(); // Keep default one for simple calls
        services.Configure<VietstockOptions>(context.Configuration.GetSection("VietStock"));
        services.AddSingleton<IVietstockSessionManager, VietstockSessionManager>();
        services.ServiceDependencies();
        services.DALDependencies(context.Configuration);
        services.AddQuartzJobs();
    })
    .UseWindowsService()
    .Build();

await host.RunAsync();
