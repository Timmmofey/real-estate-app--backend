using Microsoft.AspNetCore.Builder;

namespace Classified.Shared.Extensions.RateLimiting
{
    public static class RateLimiterExtensions
    {
        public static IApplicationBuilder UseRedisRateLimiting(
            this IApplicationBuilder app)
        {
            return app.UseMiddleware<RedisRateLimitMiddleware>();
        }
    }
}
