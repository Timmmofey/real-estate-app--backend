using Classified.Shared.Constants;

namespace Classified.Shared.Extensions.RateLimiting
{
    public sealed class RedisRateLimitAttribute : Attribute
    {
        public int PermitLimit { get; }
        public int WindowSeconds { get; }
        public RateLimitStrategy Strategy { get; }
        public JwtTokenType TokenType { get; }

        public RedisRateLimitAttribute(
            int permitLimit = 5,
            int windowSeconds = 60,
            RateLimitStrategy strategy = RateLimitStrategy.IP,
            JwtTokenType tokenType = JwtTokenType.None)
        {
            PermitLimit = permitLimit;
            WindowSeconds = windowSeconds;
            Strategy = strategy;
            TokenType = tokenType;
        }
    }
}
