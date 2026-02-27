using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Authorization;

namespace Classified.Shared.Extensions.ServerJwtAuth
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public sealed class AuthorizeServerJwtAttribute : AuthorizeAttribute, IAuthorizationFilter
    {
        private readonly string[] _allowedSubs;

        public AuthorizeServerJwtAttribute(params string[] allowedSubs)
        {
            AuthenticationSchemes = "ServerJwt";
            _allowedSubs = allowedSubs ?? Array.Empty<string>();
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            Console.WriteLine($"[AuthorizeServerJwt] IsAuthenticated={user.Identity?.IsAuthenticated}");

            if (!user.Identity?.IsAuthenticated ?? true)
            {
                Console.WriteLine("[AuthorizeServerJwt] User is not authenticated");
                context.Result = new UnauthorizedResult();
                return;
            }

            var sub = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            Console.WriteLine($"[AuthorizeServerJwt] token.sub='{sub}' allowed=[{string.Join(',', _allowedSubs)}]");

            // Нечувствительное к регистру сравнение и trim
            if (string.IsNullOrWhiteSpace(sub) || !_allowedSubs.Any(a => string.Equals(a?.Trim(), sub?.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                Console.WriteLine("[AuthorizeServerJwt] sub not allowed -> Forbid");
                context.Result = new ForbidResult();
            }
        }
    }
}
