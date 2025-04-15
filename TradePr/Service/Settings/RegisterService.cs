namespace TradePr.Service.Settings
{
    public static class RegisterService
    {
        public static void ServiceDependencies(this IServiceCollection services)
        {
            services.AddSingleton<IBinanceService, BinanceService>();
            services.AddSingleton<IBybitService, BybitService>();
            services.AddSingleton<IAPIService, APIService>();
            services.AddSingleton<ITeleService, TeleService>();
        }
    }
}
