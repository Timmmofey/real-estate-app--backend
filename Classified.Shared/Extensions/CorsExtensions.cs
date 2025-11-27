using Microsoft.Extensions.DependencyInjection;

namespace Classified.Shared.Extensions
{
    public static class CorsExtensions
    {
        public static IServiceCollection AddDefaultCors (this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins("http://localhost:3000")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            return services;
        }
    }
}
