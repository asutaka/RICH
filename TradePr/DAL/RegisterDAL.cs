using CoinUtilsPr.DAL;

namespace TradePr.DAL
{
    public static class RegisterDAL
    {
        public static void DALDependencies(this IServiceCollection services)
        {
            services.AddSingleton<ITradingRepo, TradingRepo>();
            services.AddSingleton<IConfigDataRepo, ConfigDataRepo>();
            services.AddSingleton<ISymbolRepo, SymbolRepo>();
            services.AddSingleton<IPlaceOrderTradeRepo, PlaceOrderTradeRepo>();
            services.AddSingleton<IPrepareRepo, PrepareRepo>();
        }
    }
}
