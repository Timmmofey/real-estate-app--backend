using Classified.Shared.Extensions.RateLimiting.Models;

namespace Classified.Shared.Extensions.RateLimiting.Providers
{
    public interface IRedisRateLimitProvider
    {
        Task<RateLimitResult> ConsumeAsync(
            IReadOnlyCollection<string> keys,
            int permitLimit,
            int windowSeconds);
    }
}
