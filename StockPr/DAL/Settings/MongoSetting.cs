using Microsoft.Extensions.Options;
using MongoDB.Driver;
using StockPr.Settings;

namespace StockPr.DAL.Settings
{
    public static class MongoDbDependencies
    {
        public static void AddMongoDb(this IServiceCollection services, IConfiguration configuration)
        {
            var mongoSettings = configuration.GetSection("MongoDB").Get<MongoDbSettings>();
            
            if (mongoSettings == null || string.IsNullOrEmpty(mongoSettings.ConnectionString))
            {
                throw new InvalidOperationException(
                    "MongoDB configuration không tìm thấy hoặc ConnectionString bị thiếu. " +
                    "Vui lòng kiểm tra appsettings.json có section 'MongoDB' với 'ConnectionString' và 'DatabaseName'.");
            }
            
            var client = new MongoClient(mongoSettings.ConnectionString);
            var database = client.GetDatabase(mongoSettings.DatabaseName);
            
            services.AddSingleton<IMongoDatabase>(database);
        }
    }
}
