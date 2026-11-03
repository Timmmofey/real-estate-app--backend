using Classified.Shared.Extensions.RateLimiting.Attributes;
using Classified.Shared.Extensions.RateLimiting.Models;

namespace Classified.Shared.Extensions.RateLimiting.Services
{
    public interface IRateLimitService
    {
        Task<RateLimitResult> CheckAsync(
            IReadOnlyCollection<string> keys,
            RateLimitAttribute options);
    }
}
