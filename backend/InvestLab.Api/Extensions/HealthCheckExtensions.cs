using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Driver;

namespace InvestLab.Api.Extensions
{
    public static class HealthCheckExtensions
    {
        public static IServiceCollection AddCustomHealthChecks(this IServiceCollection services, IConfiguration config)
        {
            services.AddHealthChecks()
                .AddNpgSql(
                    config.GetConnectionString("DefaultConnection")!,
                    name: "postgres",
                    failureStatus: HealthStatus.Unhealthy)
                .AddMongoDb(sp =>
                {
                    var connectionString = config["Mongo:ConnectionString"];
                    return new MongoClient(connectionString);
                },
                name: "mongo",
                failureStatus: HealthStatus.Unhealthy);

            return services;
        }
    }
}