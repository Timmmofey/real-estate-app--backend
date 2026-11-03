using Classified.Shared.Extensions.RateLimiting.Models;

namespace Classified.Shared.Extensions.RateLimiting.Providers
{
    public interface IMemoryRateLimitProvider
    {
        RateLimitResult Consume(
            IReadOnlyCollection<string> keys,
            int permitLimit,
            int windowSeconds);
        int CleanupExpired();
    }
}
