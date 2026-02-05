using ChartVisualizationPr.Services;
using StockPr.DAL;
using StockPr.DAL.Settings;
using StockPr.Service;
using StockPr.Service.Settings;
using StockPr.Settings;
using StockPr.Utils;
using StockPr.Config;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure settings from appsettings.json
builder.Services.Configure<VietStockSettings>(
    builder.Configuration.GetSection("VietStock"));
builder.Services.Configure<VietstockOptions>(
    builder.Configuration.GetSection("VietStock"));
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDB"));

// Register services
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IVietstockSessionManager, VietstockSessionManager>();
builder.Services.ServiceDependencies();
builder.Services.DALDependencies(builder.Configuration);

// Add MemoryCache for performance optimization
builder.Services.AddMemoryCache();

// Register ChartVisualization services
builder.Services.AddScoped<IChartDataService, ChartDataService>();


var app = builder.Build();

// Configure the HTTP request pipeline
app.UseCors("AllowAll");
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseHttpsRedirection();
app.MapControllers();

app.Run();
