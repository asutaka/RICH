namespace StockExtendPr.DAL.Settings
{
    public static class RegisterDAL
    {
        public static void DALDependencies(this IServiceCollection services)
        {
            services.AddSingleton<IMacroMicroRepo, MacroMicroRepo>();
            services.AddSingleton<IConfigDataRepo, ConfigDataRepo>();
        }
    }
}
