namespace CoinPr.DAL.Settings
{
    public static class RegisterDAL
    {
        public static void DALDependencies(this IServiceCollection services)
        {
            services.AddSingleton<IUserMessageRepo, UserMessageRepo>();
            services.AddSingleton<IOrderBlockRepo, OrderBlockRepo>();
            services.AddSingleton<ICoinRepo, CoinRepo>();
            services.AddSingleton<IBlackListRepo, BlackListRepo>();
        }
    }
}
