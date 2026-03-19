using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Classified.Shared.Extensions.ServerJwtAuth
{

namespace Classified.Shared.Extensions.ServerJwtAuth
    {
        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
        public class AuthorizeServerJwtBySubAttribute : AuthorizeAttribute, IAsyncAuthorizationFilter
        {
            private readonly string[] _allowedSubjects;

            public AuthorizeServerJwtBySubAttribute(params string[] allowedSubjects)
            {
                _allowedSubjects = allowedSubjects ?? throw new ArgumentNullException(nameof(allowedSubjects));
                AuthenticationSchemes = "InternalServerJwt"; 
                                                             
            }

            public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
            {
                var httpContext = context.HttpContext;

                // 1. Проверяем, что пользователь аутентифицирован (JwtBearer уже должен был это сделать)
                var user = httpContext.User;
                if (user?.Identity?.IsAuthenticated != true)
                {
                    context.Result = new UnauthorizedResult();
                    return;
                }

                // 2. Извлекаем claim 'sub' из токена
                var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

                if (string.IsNullOrWhiteSpace(sub))
                {
                    context.Result = new UnauthorizedResult();
                    return;
                }

                // 3. Проверяем, что sub входит в список разрешённых (из атрибута)
                if (!_allowedSubjects.Any(s => string.Equals(s, sub, StringComparison.OrdinalIgnoreCase)))
                {
                    context.Result = new ForbidResult();
                    return;
                }

            }
        }
    }
}
