using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Classified.Shared.Extensions.ServerJwtAuth
{
    public static class ServerJwtExtensions
    {
        public static IServiceCollection AddServerJwtAuthentication(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var key = configuration["ServerJwt:ServerKey"]
                      ?? throw new InvalidOperationException("ServerJwt:ServerKey not configured");

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(3),
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = signingKey
            };

            // Регистрируем named JwtBearer (не трогаем DefaultScheme)
            services.AddAuthentication()
                .AddJwtBearer("ServerJwt", options =>
                {
                    options.TokenValidationParameters = tokenValidationParameters;

                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var authHeader = context.Request.Headers["Authorization"].ToString();
                            if (!string.IsNullOrEmpty(authHeader) &&
                                authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                            {
                                context.Token = authHeader["Bearer ".Length..];
                            }
                            return Task.CompletedTask;
                        }
                    };
                });

            services.AddAuthorization();
            return services;
        }
    }
}