using ChartVisualizationPr.Services;
using StockPr.DAL;
using StockPr.DAL.Settings;
using StockPr.Service;
using StockPr.Service.Settings;
using StockPr.Settings;
using StockPr.Utils;

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
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDB"));

// Register only the services we need
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IAPIService, APIService>();
builder.Services.AddMongoDb(builder.Configuration);
builder.Services.AddSingleton<ISymbolRepo, SymbolRepo>();
builder.Services.AddSingleton<IPhanLoaiNDTRepo, PhanLoaiNDTRepo>();
builder.Services.AddSingleton<IStockRepo, StockRepo>();

// Add MemoryCache for performance optimization
builder.Services.AddMemoryCache();

// Register ChartVisualization services
builder.Services.AddScoped<IChartDataService, ChartDataService>();

// Initialize StaticVal with VietStock credentials from configuration
var vietStockSettings = builder.Configuration.GetSection("VietStock").Get<VietStockSettings>();
if (vietStockSettings != null)
{
    StaticVal._VietStock_Cookie = vietStockSettings.Cookie;
    StaticVal._VietStock_Token = vietStockSettings.Token;
}

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseCors("AllowAll");
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseHttpsRedirection();
app.MapControllers();

app.Run();
