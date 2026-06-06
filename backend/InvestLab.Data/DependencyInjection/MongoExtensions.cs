using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace InvestLab.Data.DependencyInjection
{
    public static class MongoExtensions
    {
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