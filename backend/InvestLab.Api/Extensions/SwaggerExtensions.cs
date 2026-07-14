using Microsoft.OpenApi.Models;
using System.Reflection;

namespace InvestLab.Api.Extensions
{
    public static class SwaggerExtensions
    {
        public static IServiceCollection AddCustomSwagger(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();

            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "InvestLab API",
                    Version = "v1",
                    Description = "API REST de InvestLab, plataforma de simulación de inversiones con datos de mercado en tiempo real. " +
                        "Expone la gestión de usuarios, portfolios virtuales, operaciones de compra/venta, alertas de precio, " +
                        "notificaciones, favoritos y consulta de mercado.\n\n" +
                        "La mayoría de los endpoints requieren autenticación mediante un token JWT (ver botón **Authorize**).",
                    Contact = new OpenApiContact
                    {
                        Name = "Rocío Díaz",
                        Url = new Uri("https://investlab-demo.com")
                    },
                    License = new OpenApiLicense
                    {
                        Name = "Proyecto Final — Instituto de Formación Técnica Superior N°24"
                    }
                });

                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Ingresá el token JWT"
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });

                var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

                options.IncludeXmlComments(xmlPath);
            });

            return services;
        }
    }
}