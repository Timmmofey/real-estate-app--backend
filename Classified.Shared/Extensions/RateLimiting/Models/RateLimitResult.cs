namespace Classified.Shared.Extensions.RateLimiting.Models
{
    public sealed record RateLimitResult(
    bool Allowed,
    long RetryAfter = 0,
    bool CapacityExceeded = false);
}

