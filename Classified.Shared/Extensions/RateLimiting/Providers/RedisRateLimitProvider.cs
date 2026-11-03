using Classified.Shared.Extensions.RateLimiting.Models;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Classified.Shared.Extensions.RateLimiting.Providers
{
    public sealed class RedisRateLimitProvider : IRedisRateLimitProvider
    {
        private readonly IServiceProvider _serviceProvider;

        private IConnectionMultiplexer? _redis;
        private readonly object _lock = new();

        private const string Script = """
        local limit = tonumber(ARGV[1])
        local window = tonumber(ARGV[2])

        for i = 1, #KEYS do
            local count = tonumber(redis.call('GET', KEYS[i]) or '0')

            if count >= limit then
                local ttl = redis.call('TTL', KEYS[i])

                if ttl < 0 then
                    ttl = window
                end

                return {0, ttl}
            end
        end

        local minTtl = window

        for i = 1, #KEYS do
            local count = redis.call('INCR', KEYS[i])

            if count == 1 then
                redis.call('EXPIRE', KEYS[i], window)
            end

            local ttl = redis.call('TTL', KEYS[i])

            if ttl >= 0 and ttl < minTtl then
                minTtl = ttl
            end
        end

        return {1, minTtl}
        """;

        public RedisRateLimitProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        private IConnectionMultiplexer GetRedis()
        {
            if (_redis is not null)
                return _redis;

            lock (_lock)
            {
                if (_redis is not null)
                    return _redis;

                _redis = _serviceProvider
                    .GetRequiredService<IConnectionMultiplexer>();

                return _redis;
            }
        }

        public async Task<RateLimitResult> ConsumeAsync(
            IReadOnlyCollection<string> keys,
            int permitLimit,
            int windowSeconds)
        {
            var redis = GetRedis();
            var db = redis.GetDatabase();

            var result = (RedisResult[])await db.ScriptEvaluateAsync(
                Script,
                keys.Select(x => (RedisKey)x).ToArray(),
                new RedisValue[]
                {
                permitLimit,
                windowSeconds
                });

            return new RateLimitResult(
                (int)result[0] == 1,
                Math.Max(1, (long)result[1]));
        }
    }
}
