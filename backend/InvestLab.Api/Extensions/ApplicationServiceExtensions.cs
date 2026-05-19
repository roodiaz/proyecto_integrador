namespace InvestLab.Api.Extensions
{
    public static class ApplicationServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration config)
        {
            services.AddApiBusinessServices();

            var connectionString = config.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connectionString))
                throw new Exception("Connection string no configurada");

            services.AddDataServices(connectionString);
            services.AddMongoServices(config);

            return services;
        }

        public static IServiceCollection AddWorkerApplicationServices(this IServiceCollection services, IConfiguration config)
        {
            services.AddWorkerBusinessServices();

            var connectionString = config.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connectionString))
                throw new Exception("Connection string no configurada");

            services.AddDataServices(connectionString);
            services.AddMongoServices(config);

            return services;
        }
    }
}