namespace TestPr.Service.Settings
{
    public static class RegisterService
    {
        public static void ServiceDependencies(this IServiceCollection services)
        {
            services.AddSingleton<ITestService, TestService>();
        }
    }
}
