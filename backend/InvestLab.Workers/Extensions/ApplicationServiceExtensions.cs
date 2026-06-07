using InvestLab.Data.DependencyInjection;

namespace InvestLab.Workers.Extensions
{
    public static class ApplicationServiceExtensions
    {

        /// <summary>
        /// Registra y configura los servicios de aplicación necesarios para el worker,
        /// incluyendo los servicios de negocio, la conexión a la base de datos y los servicios de Mongo.
        /// </summary>
        /// <param name="services">Colección de servicios sobre la que se registran las dependencias.</param>
        /// <param name="config">Configuración de la aplicación utilizada para obtener la cadena de conexión.</param>
        /// <returns>La colección de servicios con las dependencias del worker ya registradas.</returns>
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