﻿namespace TradePr.DAL.Settings
{
    public static class RegisterDAL
    {
        public static void DALDependencies(this IServiceCollection services)
        {
            services.AddSingleton<ITradingRepo, TradingRepo>();
            services.AddSingleton<IActionTradeRepo, ActionTradeRepo>();
            services.AddSingleton<ITokenUnlockRepo, TokenUnlockRepo>();
            services.AddSingleton<ITokenUnlockTradeRepo, TokenUnlockTradeRepo>();
            services.AddSingleton<IErrorPartnerRepo, ErrorPartnerRepo>();
            services.AddSingleton<IThreeSignalTradeRepo, ThreeSignalTradeRepo>();
        }
    }
}
