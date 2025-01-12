namespace StockExtendPr.Service.Settings
{
    public static class RegisterService
    {
        public static void ServiceDependencies(this IServiceCollection services)
        {
            services.AddSingleton<IAPIService, APIService>();
            services.AddSingleton<IGiaNganhHangService, GiaNganhHangService>();
        }
    }
}
