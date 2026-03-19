using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IdentityModel.Tokens.Jwt;

namespace Classified.Shared.Extensions.ServerJwtAuth
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public sealed class AuthorizeServerJwtAttribute : AuthorizeAttribute, IAsyncAuthorizationFilter
    {
        private readonly string[] _allowedIssuersForEndpoint;

        public AuthorizeServerJwtAttribute(params string[] allowedIssuersForEndpoint)
        {
            if (allowedIssuersForEndpoint == null || allowedIssuersForEndpoint.Length == 0)
                throw new ArgumentException("At least one allowed issuer must be specified", nameof(allowedIssuersForEndpoint));

            _allowedIssuersForEndpoint = allowedIssuersForEndpoint;
            AuthenticationSchemes = "InternalServerJwt"; 
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var httpContext = context.HttpContext;
            var user = httpContext.User;

            // 1. Базовая аутентификация
            if (user?.Identity?.IsAuthenticated != true)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            // 2. Извлекаем issuer
            var issuer = user.FindFirst(JwtRegisteredClaimNames.Iss)?.Value;
            if (string.IsNullOrWhiteSpace(issuer))
            {
                context.Result = new UnauthorizedResult(); // нет issuer'а
                return;
            }

            // 3. Проверка: issuer должен быть в списке разрешённых для этого эндпоинта (из атрибута)
            if (!_allowedIssuersForEndpoint.Any(i => string.Equals(i, issuer, StringComparison.OrdinalIgnoreCase)))
            {
                context.Result = new ForbidResult(); // 403 – доступ запрещён
                return;
            }

            // 4. Проверка: issuer должен быть в глобальном списке доверенных издателей (из конфига)
            var configuration = httpContext.RequestServices.GetRequiredService<IConfiguration>();
            var globalAllowedIssuers = configuration.GetSection("ServerJwt:AllowedIssuers").Get<string[]>();
            if (globalAllowedIssuers == null || !globalAllowedIssuers.Contains(issuer))
            {
                context.Result = new ForbidResult(); // 403
                return;
            }

        }
    }
}
