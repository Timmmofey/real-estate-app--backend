using Classified.Shared.Extensions.RateLimiting.Middleware;
using Classified.Shared.Extensions.RateLimiting.Providers;
using Classified.Shared.Extensions.RateLimiting.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using static Classified.Shared.Extensions.RateLimiting.Services.RateLimitService;

namespace Classified.Shared.Extensions.RateLimiting.Extensions
{
    public static class RateLimitExtensions
    {
        public static IServiceCollection AddRateLimiting(
            this IServiceCollection services)
        {
            services.AddMemoryCache();

            services.TryAddSingleton<
                IMemoryRateLimitProvider,
                MemoryRateLimitProvider>();

            services.TryAddSingleton<
                IRedisRateLimitProvider,
                RedisRateLimitProvider>();

            services.TryAddSingleton<
                IRateLimitService,
                RateLimitService>();

            services.AddHostedService<MemoryRateLimitCleanupService>();

            return services;
        }

        public static IApplicationBuilder UseRateLimiting(
            this IApplicationBuilder app)
        {
            return app.UseMiddleware<RateLimitMiddleware>();
        }
    }
}