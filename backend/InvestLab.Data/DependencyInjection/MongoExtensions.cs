using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace InvestLab.Data.DependencyInjection
{
    public static class MongoExtensions
    {
        /// <summary>
        /// Registra los servicios de MongoDB (cliente y base de datos) en el contenedor de inyección de dependencias, validando la configuración necesaria.
        /// </summary>
        /// <param name="services">Colección de servicios a la cual se agregan las dependencias.</param>
        /// <param name="config">Configuración de la aplicación desde donde se obtienen los datos de conexión a Mongo.</param>
        /// <returns>La colección de servicios con las dependencias de Mongo registradas.</returns>
        public static IServiceCollection AddMongoServices(this IServiceCollection services, IConfiguration config)
        {
            var connectionString = config["Mongo:ConnectionString"];
            var databaseName = config["Mongo:Database"];

            if (string.IsNullOrEmpty(connectionString))
                throw new Exception("Mongo connection string no configurada");

            if (string.IsNullOrEmpty(databaseName))
                throw new Exception("Mongo database no configurada");

            // Cliente Mongo (singleton)
            services.AddSingleton<IMongoClient>(_ =>
                new MongoClient(connectionString)
            );

            // Database (scoped)
            services.AddScoped<IMongoDatabase>(sp =>
            {
                var client = sp.GetRequiredService<IMongoClient>();
                return client.GetDatabase(databaseName);
            });

            return services;
        }
    }
}