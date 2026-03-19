using Classified.Shared.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace Classified.Shared.Extensions.Auth
{
    public static class AuthenticationExtensions
    {
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var key = configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key not configured");
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var signingKey = new SymmetricSecurityKey(keyBytes);

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(60),
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = signingKey
            };

            // reuse instances
            services.AddSingleton(tokenValidationParameters);
            services.AddSingleton<JwtSecurityTokenHandler>();

            // Register TokenValidationService
            services.AddSingleton<ITokenValidationService, TokenValidationService>();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "ClientJwt"; // Клиентская JWT по умолчанию
                options.DefaultChallengeScheme = "ClientJwt";
            })
            .AddJwtBearer("ClientJwt", options =>
            {
                options.TokenValidationParameters = tokenValidationParameters;

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (context.Request.Cookies.TryGetValue(CookieNames.Auth, out var token) && !string.IsNullOrEmpty(token))
                        {
                            context.Token = token;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

            services.AddAuthorization(options =>
            {
                // 1) Политики по каждому JwtTokenType — явная валидция через ITokenValidationService
                foreach (JwtTokenType tokenType in Enum.GetValues(typeof(JwtTokenType)))
                {
                    var policyName = tokenType.ToString();

                    options.AddPolicy(policyName, policy =>
                    {
                        policy.RequireAssertion(context =>
                        {
                            // безопасно достаём HttpContext
                            var httpContext = context.Resource switch
                            {
                                Microsoft.AspNetCore.Mvc.Filters.AuthorizationFilterContext mvc => mvc.HttpContext,
                                HttpContext hc => hc,
                                _ => null
                            };


                            if (httpContext == null)
                            {
                                throw new Exception("HttpContext is null");
                            }

                            if (httpContext == null)
                                return false;

                            var tokenService = httpContext.RequestServices.GetService(typeof(ITokenValidationService)) as ITokenValidationService;
                            if (tokenService == null)
                                return false;

                            // map tokenType -> cookie name
                            string? cookieName = tokenType switch
                            {
                                JwtTokenType.Access => CookieNames.Auth,
                                JwtTokenType.Refresh => CookieNames.Refresh,
                                JwtTokenType.Device => CookieNames.Device,
                                JwtTokenType.PasswordReset => CookieNames.PasswordReset,
                                JwtTokenType.TwoFactorAuthentication => CookieNames.TwoFactorAuthentication,
                                JwtTokenType.Restore => CookieNames.Restore,
                                JwtTokenType.RequestNewEmailCofirmation => CookieNames.RequestNewEmailCofirmation, 
                                JwtTokenType.EmailReset => CookieNames.EmailReset, 
                                JwtTokenType.OAuthRegistration => CookieNames.OAuthRegistration, 
                                _ => null
                            };

                            if (string.IsNullOrEmpty(cookieName))
                                return false;

                            if (!httpContext.Request.Cookies.TryGetValue(cookieName, out var token) || string.IsNullOrEmpty(token))
                                return false;

                            try
                            {
                                // Валидация токена конкретного типа. Метод должен проверять подпись/lifetime и возвращать ClaimsPrincipal.
                                var principal = tokenService.ValidateAndGetPrincipal(token, tokenType);
                                if (principal == null)
                                    return false;

                                // проверяем claim "type" на соответствие
                                if (principal.FindFirst("type")?.Value != policyName)
                                    return false;

                                // Проставляем User чтобы контроллеры могли читать User.Claims, если нужно
                                httpContext.User = principal;

                                return true;
                            }
                            catch
                            {
                                // не логируем здесь — можно логировать внутри tokenService
                                return false;
                            }
                        });
                    });
                }

                // 2) Политика Device + Refresh — явная проверка обеих cookie
                options.AddPolicy("DeviceAndRefreshOnly", policy =>
                {
                    policy.RequireAssertion(context =>
                    {
                        var httpContext = context.Resource switch
                        {
                            Microsoft.AspNetCore.Mvc.Filters.AuthorizationFilterContext mvc => mvc.HttpContext,
                            HttpContext hc => hc,
                            _ => null
                        };

                        if (httpContext == null)
                            return false;

                        var tokenService = httpContext.RequestServices.GetService(typeof(ITokenValidationService)) as ITokenValidationService;
                        if (tokenService == null)
                            return false;

                        if (!httpContext.Request.Cookies.TryGetValue(CookieNames.Device, out var deviceToken) || string.IsNullOrEmpty(deviceToken))
                            return false;
                        if (!httpContext.Request.Cookies.TryGetValue(CookieNames.Refresh, out var refreshToken) || string.IsNullOrEmpty(refreshToken))
                            return false;

                        try
                        {
                            var devicePrincipal = tokenService.ValidateAndGetPrincipal(deviceToken, JwtTokenType.Device);
                            var refreshPrincipal = tokenService.ValidateAndGetPrincipal(refreshToken, JwtTokenType.Refresh);

                            if (devicePrincipal == null || refreshPrincipal == null)
                                return false;

                            if (devicePrincipal.FindFirst("type")?.Value != JwtTokenType.Device.ToString())
                                return false;
                            if (refreshPrincipal.FindFirst("type")?.Value != JwtTokenType.Refresh.ToString())
                                return false;

                            // Можно выставить "User" — но осторожно: что имеет смысл в контексте этой политики?
                            // Здесь оставим httpContext.User = devicePrincipal; // если нужно — меняй по необходимости
                            httpContext.User = devicePrincipal;

                            return true;
                        }
                        catch
                        {
                            return false;
                        }
                    });
                });

                // ВАЖНО: НЕ устанавливаем DefaultPolicy — публичные endpoint'ы должны оставаться публичными.
            });

            return services;
        }
    }
}
