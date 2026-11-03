using Classified.Shared.Constants;
using Classified.Shared.Extensions.Auth;
using Classified.Shared.Extensions.RateLimiting.Attributes;
using Classified.Shared.Extensions.RateLimiting.Enums;
using Classified.Shared.Extensions.RateLimiting.Services;
using Classified.Shared.Infrastructure.JWT;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using StackExchange.Redis;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Classified.Shared.Extensions.RateLimiting.Middleware;

public sealed class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IRateLimitService _rateLimitService;
    private readonly ITokenValidationService _tokenService;

    public RateLimitMiddleware(
        RequestDelegate next,
        IRateLimitService rateLimitService,
        ITokenValidationService tokenService)
    {
        _next = next;
        _rateLimitService = rateLimitService;
        _tokenService = tokenService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();

        var options = endpoint?
            .Metadata
            .GetMetadata<RateLimitAttribute>();

        if (options is null)
        {
            await _next(context);
            return;
        }

        try
        {
            var keys = BuildKeys(context, endpoint!, options);

            var result = await _rateLimitService.CheckAsync(
                keys,
                options);

            if (!result.Allowed)
            {
                context.Response.Headers.RetryAfter =
                    Math.Max(1, result.RetryAfter).ToString();

                await Reject(
                    context,
                    StatusCodes.Status429TooManyRequests,
                    "TooManyRequests",
                    "Too many requests. Please try again later.");

                return;
            }

            await _next(context);
        }
        catch (RedisException)
        {
            await Reject(
                context,
                StatusCodes.Status503ServiceUnavailable,
                "ServiceUnavailable",
                "Rate limiting service is unavailable.");
        }
    }

    private IReadOnlyCollection<string> BuildKeys(
        HttpContext context,
        Endpoint endpoint,
        RateLimitAttribute options)
    {
        var endpointKey =
            (endpoint as RouteEndpoint)?.RoutePattern?.RawText
            ?? endpoint.DisplayName
            ?? "unknown";

        var endpointHash = Hash(endpointKey);

        var keys = new List<string>();

        if (options.Strategy == RateLimitStrategy.Global)
        {
            keys.Add($"rate_limit:{endpointHash}:global");

            return keys;
        }

        if (options.Strategy is RateLimitStrategy.User
            or RateLimitStrategy.UserAndIP)
        {
            var userId = GetUserId(context, options.TokenType);

            if (string.IsNullOrWhiteSpace(userId))
                throw new RateLimitUserNotFoundException();

            keys.Add(
                $"rate_limit:{endpointHash}:user:{Hash(userId)}");
        }

        if (options.Strategy is RateLimitStrategy.IP
            or RateLimitStrategy.UserAndIP)
        {
            var ip = context.Connection.RemoteIpAddress?.ToString()
                     ?? "unknown";

            keys.Add(
                $"rate_limit:{endpointHash}:ip:{Hash(ip)}");
        }

        return keys;
    }

    private string? GetUserId(
        HttpContext context,
        JwtTokenType tokenType)
    {
        var cookieName = tokenType switch
        {
            JwtTokenType.Access => CookieNames.Auth,
            JwtTokenType.Refresh => CookieNames.Refresh,
            JwtTokenType.Device => CookieNames.Device,
            JwtTokenType.Restore => CookieNames.Restore,
            JwtTokenType.PasswordReset => CookieNames.PasswordReset,
            JwtTokenType.TwoFactorAuthentication =>
                CookieNames.TwoFactorAuthentication,
            JwtTokenType.RequestNewEmailCofirmation =>
                CookieNames.RequestNewEmailCofirmation,
            JwtTokenType.EmailReset => CookieNames.EmailReset,
            JwtTokenType.OAuthRegistration => CookieNames.OAuthRegistration,
            _ => null
        };

        if (cookieName is null)
            return null;

        if (!context.Request.Cookies.TryGetValue(
                cookieName, out var token)
            || string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        try
        {
            var principal = _tokenService.ValidateAndGetPrincipal(
                token,
                tokenType);

            if (principal.FindFirst("type")?.Value
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

    private static async Task Reject(
        HttpContext context,
        int statusCode,
        string status,
        string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(new
        {
            Message = message,
            Status = status
        });
    }

    private static string Hash(string value)
    {
        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }
}

public sealed class RateLimitUserNotFoundException : Exception;
