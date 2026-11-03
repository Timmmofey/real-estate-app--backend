using Classified.Shared.Constants;
using Classified.Shared.Extensions.RateLimiting.Enums;

namespace Classified.Shared.Extensions.RateLimiting.Attributes
{
    [AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Method,
    AllowMultiple = false,
    Inherited = true)]
    public sealed class RateLimitAttribute : Attribute
    {
        public int PermitLimit { get; }
        public int WindowSeconds { get; }

        public RateLimitStrategy Strategy { get; }
        public RateLimitStorage Storage { get; }

        public JwtTokenType TokenType { get; }

        public int MemoryPermitLimit { get; }
        public int MemoryWindowSeconds { get; }

        public RateLimitAttribute(
            int permitLimit,
            int windowSeconds,
            RateLimitStrategy strategy = RateLimitStrategy.IP,
            JwtTokenType tokenType = JwtTokenType.Access,
            RateLimitStorage storage = RateLimitStorage.Redis,
            int memoryPermitLimit = 0,
            int memoryWindowSeconds = 0)
        {
            if (permitLimit <= 0)
                throw new ArgumentOutOfRangeException(nameof(permitLimit));

            if (windowSeconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(windowSeconds));

            if (memoryPermitLimit < 0)
                throw new ArgumentOutOfRangeException(nameof(memoryPermitLimit));

            if (memoryWindowSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(memoryWindowSeconds));

            PermitLimit = permitLimit;
            WindowSeconds = windowSeconds;

            Strategy = strategy;
            TokenType = tokenType;
            Storage = storage;

            MemoryPermitLimit = memoryPermitLimit;
            MemoryWindowSeconds = memoryWindowSeconds;
        }
    }
}
