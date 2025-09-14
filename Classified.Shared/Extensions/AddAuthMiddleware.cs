using Classified.Shared.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace Classified.Shared.Extensions
{
    public static class AuthenticationExtensions
    {

        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {


            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!))
                };

                //options.Events = new JwtBearerEvents
                //{
                //    OnMessageReceived = context =>
                //    {
                //        if (context.Request.Cookies.TryGetValue("classified-auth-token", out var accessToken))
                //        {
                //            context.Token = accessToken;
                //        }
                //        else if (context.Request.Cookies.TryGetValue("classified-refresh-token", out var refreshToken))
                //        {
                //            context.Token = refreshToken;
                //        }

                //        return Task.CompletedTask;
                //    }
                //};

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var handler = new JwtSecurityTokenHandler();

                        // Список кук, которые могут содержать JWT
                        var cookiesToCheck = new[]
                        {
                            CookieNames.Auth,
                            CookieNames.Refresh,
                            CookieNames.Device,
                            CookieNames.Restore,
                            CookieNames.PasswordReset,
                            CookieNames.RequestNewEmailCofirmation,
                            CookieNames.EmailReset
                        };

                        foreach (var cookieName in cookiesToCheck)
                        {
                            if (context.Request.Cookies.TryGetValue(cookieName, out var token) &&
                                !string.IsNullOrEmpty(token))
                            {
                                try
                                {
                                    var jwt = handler.ReadJwtToken(token);
                                    var typeClaim = jwt.Claims.FirstOrDefault(c => c.Type == "type")?.Value;

                                    if (!string.IsNullOrEmpty(typeClaim) &&
                                        Enum.TryParse<JwtTokenType>(typeClaim, true, out var tokenType))
                                    {
                                        context.Token = token;
                                        break;
                                    }
                                }
                                catch
                                {
                                    continue;
                                }
                            }
                        }

                        return Task.CompletedTask;
                    }
                };
            });

            return services;
        }
    }
}
