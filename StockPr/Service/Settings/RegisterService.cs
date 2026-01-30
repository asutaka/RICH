using StockPr.Research;

namespace StockPr.Service.Settings
{
    public static class RegisterService
    {
        public static void ServiceDependencies(this IServiceCollection services)
        {
            services.AddSingleton<IAPIService, APIService>();
            services.AddSingleton<IAnalyzeService, AnalyzeService>();
            services.AddSingleton<IBaoCaoPhanTichService, BaoCaoPhanTichService>();
            services.AddSingleton<IChartService, ChartService>();
            services.AddSingleton<ICommonService, CommonService>();
            services.AddSingleton<IEPSRankService, EPSRankService>();
            services.AddSingleton<IF319Service, F319Service>();
            services.AddSingleton<IGiaNganhHangService, GiaNganhHangService>();
            services.AddSingleton<IMessageService, MessageService>();
            services.AddSingleton<INewsService, NewsService>();
            services.AddSingleton<IPortfolioService, PortfolioService>();
            services.AddSingleton<ITeleService, TeleService>();
            services.AddSingleton<ITongCucThongKeService, TongCucThongKeService>();
            services.AddSingleton<IBaoCaoTaiChinhService, BaoCaoTaiChinhService>();
            services.AddSingleton<ITuDoanhService, TuDoanhService>();
            services.AddSingleton<IFileService, FileService>();
            services.AddSingleton<IAIService, AIService>();
            services.AddSingleton<IVietstockAuthService, VietstockAuthService>();
            
            // Research services (for backtesting)
            services.AddSingleton<IBacktestService, BacktestService>();
        }
    }
}
