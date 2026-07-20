using Microsoft.OpenApi.Models;

namespace MutualFundNav.API.Extensions
{
    public static class SwaggerExtensions
    {
        public static IServiceCollection AddSwaggerDocs(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "MutualFundNav API",
                    Version = "v1",
                    Description = "NAV downloader service — downloads AMFI mutual fund NAV data " +
                                  "daily and publishes events to Kafka.",
                    Contact = new OpenApiContact
                    {
                        Name = "AMFINAV Team",
                        Email = "admin@amfinav.io"
                    }
                });

                // Include XML comments
                var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                    c.IncludeXmlComments(xmlPath);
            });

            return services;
        }
    }
}
