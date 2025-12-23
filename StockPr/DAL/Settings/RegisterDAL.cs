namespace StockPr.DAL.Settings
{
    public static class RegisterDAL
    {
        public static void DALDependencies(this IServiceCollection services, IConfiguration configuration)
        {
            // Register MongoDB
            services.AddMongoDb(configuration);
            
            services.AddSingleton<IUserMessageRepo, UserMessageRepo>();
            services.AddSingleton<IStockRepo, StockRepo>();
            services.AddSingleton<IFinancialRepo, FinancialRepo>();
            services.AddSingleton<IConfigDataRepo, ConfigDataRepo>();
            services.AddSingleton<IThongKeRepo, ThongKeRepo>();
            services.AddSingleton<IConfigBaoCaoPhanTichRepo, ConfigBaoCaoPhanTichRepo>();
            services.AddSingleton<IConfigF319Repo, ConfigF319Repo>();
            services.AddSingleton<ICategoryRepo, CategoryRepo>();
            services.AddSingleton<IMacroMicroRepo, MacroMicroRepo>();
            services.AddSingleton<IConfigPortfolioRepo, ConfigPortfolioRepo>();
            services.AddSingleton<ISymbolRepo, SymbolRepo>();
            services.AddSingleton<IF319AccountRepo, F319AccountRepo>();
            services.AddSingleton<IConfigNewsRepo, ConfigNewsRepo>();
            services.AddSingleton<IAccountRepo, AccountRepo>();
            services.AddSingleton<IPhanLoaiNDTRepo, PhanLoaiNDTRepo>();
            services.AddSingleton<IPreEntryRepo, PreEntryRepo>();
        }
    }
}
