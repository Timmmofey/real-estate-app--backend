using Classified.Shared.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.IdentityModel.Tokens.Jwt;

namespace Classified.Shared.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class AuthorizeTokenAttribute : Attribute, IAuthorizationFilter
    {
        private readonly JwtTokenType _tokenType;

        public AuthorizeTokenAttribute(JwtTokenType tokenType)
        {
            _tokenType = tokenType;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var httpContext = context.HttpContext;

            string cookieName = _tokenType switch
            {
                JwtTokenType.Access => CookieNames.Auth,
                JwtTokenType.Refresh => CookieNames.Refresh,
                JwtTokenType.Device => CookieNames.Device,
                JwtTokenType.Restore => CookieNames.Restore,
                JwtTokenType.PasswordReset => CookieNames.PasswordReset,
                JwtTokenType.RequestNewEmailCofirmation => CookieNames.RequestNewEmailCofirmation,
                JwtTokenType.EmailReset => CookieNames.EmailReset,
                JwtTokenType.TwoFactorAuthentication => CookieNames.TwoFactorAuthentication,
                JwtTokenType.OAuthRegistration => CookieNames.OAuthRegistration,
                _ => throw new ArgumentOutOfRangeException()
            };

            if (!httpContext.Request.Cookies.TryGetValue(cookieName, out var token) || string.IsNullOrEmpty(token))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(token);

                var tokenTypeClaim = jwt.Claims.FirstOrDefault(c => c.Type == "type")?.Value;

                if (string.IsNullOrEmpty(tokenTypeClaim) ||
                    !string.Equals(tokenTypeClaim, _tokenType.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    context.Result = new ForbidResult();
                }
            }
            catch
            {
                context.Result = new UnauthorizedResult();
            }
        }
    }
}
