namespace TradePr.DAL.Settings
{
    public static class RegisterDAL
    {
        public static void DALDependencies(this IServiceCollection services)
        {
            services.AddSingleton<ITradingRepo, TradingRepo>();
            services.AddSingleton<IConfigDataRepo, ConfigDataRepo>();
            services.AddSingleton<ISymbolRepo, SymbolRepo>();
            services.AddSingleton<IPlaceOrderTradeRepo, PlaceOrderTradeRepo>();
        }
    }
}
