using Classified.Shared.Extensions.RateLimiting.Attributes;
using Classified.Shared.Extensions.RateLimiting.Enums;
using Classified.Shared.Extensions.RateLimiting.Models;
using Classified.Shared.Extensions.RateLimiting.Providers;

namespace Classified.Shared.Extensions.RateLimiting.Services
{
    public sealed partial class RateLimitService : IRateLimitService
    {
        private readonly IMemoryRateLimitProvider _memory;
        private readonly IRedisRateLimitProvider _redis;

        public RateLimitService(
            IMemoryRateLimitProvider memory,
            IRedisRateLimitProvider redis)
        {
            _memory = memory;
            _redis = redis;
        }

        public async Task<RateLimitResult> CheckAsync(
            IReadOnlyCollection<string> keys,
            RateLimitAttribute options)
        {
            switch (options.Storage)
            {
                case RateLimitStorage.Memory:
                    return _memory.Consume(
                        keys,
                        options.PermitLimit,
                        options.WindowSeconds);

                case RateLimitStorage.Redis:
                    return await _redis.ConsumeAsync(
                        keys,
                        options.PermitLimit,
                        options.WindowSeconds);

                case RateLimitStorage.MemoryAndRedis:

                    var memoryLimit = options.MemoryPermitLimit > 0
                        ? options.MemoryPermitLimit
                        : options.PermitLimit;

                    var memoryWindow = options.MemoryWindowSeconds > 0
                        ? options.MemoryWindowSeconds
                        : options.WindowSeconds;

                    var memoryResult = _memory.Consume(
                        keys,
                        memoryLimit,
                        memoryWindow);

                    if (!memoryResult.Allowed)
                        return memoryResult;

                    return await _redis.ConsumeAsync(
                        keys,
                        options.PermitLimit,
                        options.WindowSeconds);

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
