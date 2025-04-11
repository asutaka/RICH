﻿namespace TestPr.DAL.Settings
{
    public static class RegisterDAL
    {
        public static void DALDependencies(this IServiceCollection services)
        {
            services.AddSingleton<ITokenUnlockRepo, TokenUnlockRepo>();
            services.AddSingleton<ISymbolRepo, SymbolRepo>();
        }
    }
}
