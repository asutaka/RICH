namespace StockPr.DAL.Settings
{
    public static class RegisterDAL
    {
        public static void DALDependencies(this IServiceCollection services)
        {
            services.AddSingleton<IUserMessageRepo, UserMessageRepo>();
            services.AddSingleton<IStockRepo, StockRepo>();
            services.AddSingleton<IFinancialRepo, FinancialRepo>();
            services.AddSingleton<IConfigDataRepo, ConfigDataRepo>();
            services.AddSingleton<IThongKeRepo, ThongKeRepo>();
            services.AddSingleton<IThongKeQuyRepo, ThongKeQuyRepo>();
        }
    }
}
