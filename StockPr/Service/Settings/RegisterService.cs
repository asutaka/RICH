namespace StockPr.Service.Settings
{
    public static class RegisterService
    {
        public static void ServiceDependencies(this IServiceCollection services)
        {
            services.AddSingleton<IMessageService, MessageService>();
            services.AddSingleton<ICommonService, CommonService>();
            services.AddSingleton<ITeleService, TeleService>();
            services.AddSingleton<IChartService, ChartService>();
            services.AddSingleton<IAPIService, APIService>();
            services.AddSingleton<IBaoCaoPhanTichService, BaoCaoPhanTichService>();
            services.AddSingleton<IGiaNganhHangService, GiaNganhHangService>();
        }
    }
}
