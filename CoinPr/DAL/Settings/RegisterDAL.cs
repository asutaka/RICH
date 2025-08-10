using CoinUtilsPr.DAL;

namespace CoinPr.DAL.Settings
{
    public static class RegisterDAL
    {
        public static void DALDependencies(this IServiceCollection services)
        {
            services.AddSingleton<IUserMessageRepo, UserMessageRepo>();
            services.AddSingleton<ITradingRepo, TradingRepo>();
            services.AddSingleton<ITokenUnlockRepo, TokenUnlockRepo>();
            services.AddSingleton<ITokenUnlockRepo, TokenUnlockRepo>();
            services.AddSingleton<IPrepareRepo, PrepareRepo>();
        }
    }
}
