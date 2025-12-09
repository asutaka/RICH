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
            services.AddSingleton<ITongCucThongKeService, TongCucThongKeService>();
            services.AddSingleton<IAnalyzeService, AnalyzeService>();
            services.AddSingleton<ITuDoanhService, TuDoanhService>();
            services.AddSingleton<IFileService, FileService>();
            services.AddSingleton<IBaoCaoTaiChinhService, BaoCaoTaiChinhService>();
            services.AddSingleton<IPortfolioService, PortfolioService>();
            services.AddSingleton<IEPSRankService, EPSRankService>();
            services.AddSingleton<ITestService, TestService>();
            services.AddSingleton<IF319Service, F319Service>();
            services.AddSingleton<INewsService, NewsService>();
            services.AddSingleton<IAIService, AIService>();
        }
    }
}
