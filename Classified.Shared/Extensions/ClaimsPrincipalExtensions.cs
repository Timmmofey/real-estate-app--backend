using Classified.Shared.Constants;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Classified.Shared.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static Guid GetUserId(this HttpRequest request, string? cookieName = null)
        {
            if (!string.IsNullOrEmpty(cookieName))
            {
                // Ищем userId строго в указанной cookie
                if (!request.Cookies.TryGetValue(cookieName, out var tokenHeader))
                    throw new UnauthorizedAccessException($"Missing token cookie: {cookieName}.");

                var handler = new JwtSecurityTokenHandler();
                JwtSecurityToken jwt;
                try
                {
                    jwt = handler.ReadJwtToken(tokenHeader);
                }
                catch
                {
                    throw new UnauthorizedAccessException($"Invalid token format in cookie: {cookieName}.");
                }

                var claim = jwt.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
                if (!Guid.TryParse(claim, out var userIdFromCookie))
                    throw new UnauthorizedAccessException($"Invalid or missing Id claim in cookie: {cookieName}.");

                return userIdFromCookie;
            }
            else
            {
                // Cookie не указана → используем access token
                var user = request.HttpContext.User;
                var userClaim = user.FindFirst("userId")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!Guid.TryParse(userClaim, out var userIdFromAccessToken))
                    throw new UnauthorizedAccessException("UserId not found in access token.");

                return userIdFromAccessToken;
            }
        }

        public static UserRole GetUserRole(this HttpRequest request, string? cookieName = null)
        {
            if (!string.IsNullOrEmpty(cookieName))
            {
                // Чтение роли из указанной cookie (JWT)
                if (!request.Cookies.TryGetValue(cookieName, out var tokenHeader))
                    throw new UnauthorizedAccessException($"Missing token cookie: {cookieName}.");

                var handler = new JwtSecurityTokenHandler();
                JwtSecurityToken jwt;

                try
                {
                    jwt = handler.ReadJwtToken(tokenHeader);
                }
                catch
                {
                    throw new UnauthorizedAccessException($"Invalid token format in cookie: {cookieName}.");
                }

                var roleClaim = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

                if (!Enum.TryParse<UserRole>(roleClaim, out var userRole))
                    throw new UnauthorizedAccessException($"Invalid or missing Role claim in cookie: {cookieName}.");

                return userRole;
            }
            else
            {
                // Чтение роли из access token
                var user = request.HttpContext.User;

                var roleClaim = user.FindFirst(ClaimTypes.Role)?.Value;

                if (!Enum.TryParse<UserRole>(roleClaim, out var userRole))
                    throw new UnauthorizedAccessException("Role not found or invalid in access token.");

                return userRole;
            }
        }

        public static string GetEmailFromEmailResetCookie(this HttpRequest request, string cookieName = CookieNames.EmailReset)
        {
            if (!request.Cookies.TryGetValue(cookieName, out var tokenHeader))
                throw new UnauthorizedAccessException($"Missing token cookie: {cookieName}.");

            var handler = new JwtSecurityTokenHandler();
            JwtSecurityToken jwt;

            try
            {
                jwt = handler.ReadJwtToken(tokenHeader);
            }
            catch
            {
                throw new UnauthorizedAccessException($"Invalid token format in cookie: {cookieName}.");
            }

            var emailClaim = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            if (string.IsNullOrWhiteSpace(emailClaim))
                throw new UnauthorizedAccessException($"Missing email claim in cookie: {cookieName}.");

            return emailClaim;
        }
    }
}
