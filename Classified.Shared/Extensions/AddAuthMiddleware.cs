using Classified.Shared.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Text;

namespace Classified.Shared.Extensions
{

    //СТАРЫЙ НЕОПТИМАЛЬНЫЙ МЕТОД НО ПУСТЬ БУДЕТ НА ВСЯКИЙ СЛУЧАЙ

    //public static class AuthenticationExtensions
    //{

    //    // Вычисляем все константы CookieNames один раз при старте
    //    private static readonly string[] CookiesToCheck = typeof(CookieNames)
    //        .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
    //        .Where(f => f.IsLiteral && !f.IsInitOnly)
    //        .Select(f => f.GetRawConstantValue())
    //        .OfType<string>()
    //        .ToArray();


    //    public static IServiceCollection AddJwtAuthenticationOld(this IServiceCollection services, IConfiguration configuration)
    //    {


    //        services.AddAuthentication(options =>
    //        {
    //            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    //            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    //        })
    //        .AddJwtBearer(options =>
    //        {
    //            options.TokenValidationParameters = new TokenValidationParameters
    //            {
    //                ValidateIssuer = false,
    //                ValidateAudience = false,
    //                ValidateLifetime = true,
    //                ValidateIssuerSigningKey = true,
    //                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!))
    //            };

                
    //            options.Events = new JwtBearerEvents
    //            {
    //                OnMessageReceived = context =>
    //                {
    //                    var handler = new JwtSecurityTokenHandler();

                       
    //                    // Список кук, которые могут содержать JWT
    //                    //var cookiesToCheck = new[]
    //                    //{
    //                    //    CookieNames.Auth,
    //                    //    CookieNames.Refresh,
    //                    //    CookieNames.Device,
    //                    //    CookieNames.Restore,
    //                    //    CookieNames.PasswordReset,
    //                    //    CookieNames.RequestNewEmailCofirmation,
    //                    //    CookieNames.EmailReset
    //                    //};

    //                    foreach (var cookieName in CookiesToCheck)
    //                    {
    //                        if (context.Request.Cookies.TryGetValue(cookieName!, out var token) &&
    //                            !string.IsNullOrEmpty(token))
    //                        {
    //                            try
    //                            {
    //                                var jwt = handler.ReadJwtToken(token);
    //                                var typeClaim = jwt.Claims.FirstOrDefault(c => c.Type == "type")?.Value;

    //                                if (!string.IsNullOrEmpty(typeClaim) &&
    //                                    Enum.TryParse<JwtTokenType>(typeClaim, true, out var tokenType) && tokenType == JwtTokenType.Access)
    //                                {
    //                                    context.Token = token;
    //                                    break;
    //                                }
    //                            }
    //                            catch
    //                            {
    //                                continue;
    //                            }
    //                        }
    //                    }

    //                    return Task.CompletedTask;
    //                }
    //            };
    //        });

    //        return services;
    //    }
    //}
}
