namespace TradePr.DAL.Settings
{
    public static class RegisterDAL
    {
        public static void DALDependencies(this IServiceCollection services)
        {
            services.AddSingleton<ITradingRepo, TradingRepo>();
            services.AddSingleton<ITokenUnlockRepo, TokenUnlockRepo>();
            services.AddSingleton<ITokenUnlockTradeRepo, TokenUnlockTradeRepo>();
            services.AddSingleton<IErrorPartnerRepo, ErrorPartnerRepo>();
            services.AddSingleton<IThreeSignalTradeRepo, ThreeSignalTradeRepo>();
            services.AddSingleton<ISignalTradeRepo, SignalTradeRepo>();
            services.AddSingleton<IConfigDataRepo, ConfigDataRepo>();
            services.AddSingleton<IPrepareTradeRepo, PrepareTradeRepo>();
            services.AddSingleton<IMa20TradeRepo, Ma20TradeRepo>();
            services.AddSingleton<ISymbolRepo, SymbolRepo>();
            services.AddSingleton<ISymbolConfigRepo, SymbolConfigRepo>();
        }
    }
}
