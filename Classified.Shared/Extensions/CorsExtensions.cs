using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Classified.Shared.Extensions
{
    public static class CorsExtensions
    {
        public static IServiceCollection AddDefaultCors (this IServiceCollection services, IConfiguration configuration)
        {
            var origins = configuration
               .GetSection("Cors:AllowedOrigins")
               .Get<string[]>();

            services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins(origins!)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            return services;
        }
    }
}
