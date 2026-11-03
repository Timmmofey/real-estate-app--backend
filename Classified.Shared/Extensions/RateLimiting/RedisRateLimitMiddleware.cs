using Classified.Shared.Constants;
using Classified.Shared.Extensions.Auth;
using Classified.Shared.Infrastructure.JWT;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using StackExchange.Redis;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Classified.Shared.Extensions.RateLimiting;

public sealed class RedisRateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConnectionMultiplexer _redis;
    private readonly ITokenValidationService _tokenService;

    private const string Script = """
        local limit = tonumber(ARGV[1])
        local window = tonumber(ARGV[2])

        for i = 1, #KEYS do
            local count = tonumber(redis.call('GET', KEYS[i]) or '0')

            if count >= limit then
                local ttl = redis.call('TTL', KEYS[i])
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

    public RedisRateLimitMiddleware(
        RequestDelegate next,
        IConnectionMultiplexer redis,
        ITokenValidationService tokenService)
    {
        _next = next;
        _redis = redis;
        _tokenService = tokenService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();

        var rateLimit = endpoint?
            .Metadata
            .GetMetadata<RedisRateLimitAttribute>();

        if (rateLimit is null)
        {
            await _next(context);
            return;
        }

        string? userId = null;

        if (rateLimit.Strategy is RateLimitStrategy.User
            or RateLimitStrategy.UserAndIP)
        {
            if (rateLimit.TokenType == JwtTokenType.None)
            {
                throw new InvalidOperationException(
                    "TokenType is required for user-based rate limiting.");
            }

            userId = GetUserId(context, rateLimit.TokenType);

            if (string.IsNullOrWhiteSpace(userId))
            {
                context.Response.StatusCode =
                    StatusCodes.Status401Unauthorized;

                await context.Response.WriteAsJsonAsync(new
                {
                    Message = "Unable to identify user.",
                    Status = "Unauthorized"
                });

                return;
            }
        }

        var ip = context.Connection.RemoteIpAddress?.ToString()
                 ?? "unknown";

        var endpointKey =
            (endpoint as RouteEndpoint)?.RoutePattern?.RawText
            ?? endpoint?.DisplayName
            ?? "unknown";

        var endpointHash = Hash(endpointKey);

        var keys = new List<RedisKey>();

        if (rateLimit.Strategy is RateLimitStrategy.User
            or RateLimitStrategy.UserAndIP)
        {
            keys.Add(
                $"rate_limit:{endpointHash}:user:{Hash(userId!)}");
        }

        if (rateLimit.Strategy is RateLimitStrategy.IP
            or RateLimitStrategy.UserAndIP)
        {
            keys.Add(
                $"rate_limit:{endpointHash}:ip:{Hash(ip)}");
        }

        if (keys.Count == 0)
        {
            await _next(context);
            return;
        }

        var db = _redis.GetDatabase();

        var result = (RedisResult[])await db.ScriptEvaluateAsync(
            Script,
            keys.ToArray(),
            new RedisValue[]
            {
                rateLimit.PermitLimit,
                rateLimit.WindowSeconds
            });

        var allowed = (int)result[0] == 1;
        var ttl = (long)result[1];

        if (!allowed)
        {
            context.Response.StatusCode =
                StatusCodes.Status429TooManyRequests;

            context.Response.ContentType = "application/json";

            context.Response.Headers.RetryAfter =
                Math.Max(1, ttl).ToString();

            await context.Response.WriteAsJsonAsync(new
            {
                Message = "Too many requests. Please try again later.",
                Status = "TooManyRequests"
            });

            return;
        }

        await _next(context);
    }

    private string? GetUserId(
        HttpContext context,
        JwtTokenType tokenType)
    {
        var cookieName =
            JwtTokenCookieMapper.GetCookieName(tokenType);

        if (cookieName is null)
            return null;

        if (!context.Request.Cookies.TryGetValue(
                cookieName, out var token) ||
            string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        try
        {
            var principal = _tokenService.ValidateAndGetPrincipal(
                token,
                tokenType);

            if (principal?.FindFirst("type")?.Value
                != tokenType.ToString())
            {
                return null;
            }

            return principal.FindFirst("userId")?.Value
                   ?? principal.FindFirst(
                       ClaimTypes.NameIdentifier)?.Value;
        }
        catch
        {
            return null;
        }
    }

    private static string Hash(string value)
    {
        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }
}