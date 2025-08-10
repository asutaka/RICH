using CoinUtilsPr;

namespace CoinPr.Service.Settings
{
    public static class RegisterService
    {
        public static void ServiceDependencies(this IServiceCollection services)
        {
            services.AddSingleton<IMessageService, MessageService>();
            services.AddSingleton<ITeleService, TeleService>();
            services.AddSingleton<IAPIService, APIService>();
            services.AddSingleton<IWebSocketService, WebSocketService>();
            services.AddSingleton<IPrepareService, PrepareService>();
        }
    }
}
