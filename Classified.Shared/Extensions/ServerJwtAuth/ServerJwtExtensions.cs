using Microsoft.AspNetCore.Authorization;
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

            var issuer = configuration["ServerJwt:Issuer"]
                ?? throw new InvalidOperationException("ServerJwt:Issuer is not configured");

            var allowedIssuers = configuration.GetSection("ServerJwt:AllowedIssuers").Get<string[]>()
                                 ?? throw new InvalidOperationException("ServerJwt:AllowedIssuers not configured");

            // JWT Authentication
            services.AddAuthentication()
            .AddJwtBearer("InternalServerJwt", options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuers = allowedIssuers,
                    ValidateAudience = true,
                    ValidAudience = issuer,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey
                };
            });

            //Authorization Policy
            services.AddAuthorization(options =>
            {
                options.AddPolicy("InternalServerJwt", policy =>
                {
                    policy.RequireAuthenticatedUser();
                });
            });


            return services;
        }
    }

}