﻿namespace TestPr.DAL.Settings
{
    public static class RegisterDAL
    {
        public static void DALDependencies(this IServiceCollection services)
        {
            services.AddSingleton<ITradingRepo, TradingRepo>();
            services.AddSingleton<ITokenUnlockRepo, TokenUnlockRepo>();
        }
    }
}
