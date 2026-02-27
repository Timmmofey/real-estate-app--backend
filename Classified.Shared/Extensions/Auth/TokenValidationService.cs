using Classified.Shared.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Classified.Shared.Extensions.Auth
{
    public interface ITokenValidationService
    {
        ClaimsPrincipal ValidateAndGetPrincipal(string token, JwtTokenType expectedType);
        ClaimsPrincipal? ValidateTokenByType(JwtTokenType tokenType, IRequestCookieCollection cookies);
    }

    public class TokenValidationService : ITokenValidationService
    {
        private readonly JwtSecurityTokenHandler _handler;
        private readonly TokenValidationParameters _validationParameters;


        public TokenValidationService(JwtSecurityTokenHandler handler, TokenValidationParameters validationParameters, IConfiguration configuration)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
            _validationParameters = validationParameters ?? throw new ArgumentNullException(nameof(validationParameters));
        }

        public ClaimsPrincipal ValidateAndGetPrincipal(string token, JwtTokenType expectedType)
        {
            if (string.IsNullOrEmpty(token))
                throw new SecurityTokenException("Token is null or empty");

            var principal = _handler.ValidateToken(token, _validationParameters, out var validatedToken);

            // Additional checks: expected type claim
            var typeClaim = principal.Claims.FirstOrDefault(c => string.Equals(c.Type, "type", StringComparison.OrdinalIgnoreCase))?.Value;
            if (!string.Equals(typeClaim, expectedType.ToString(), StringComparison.OrdinalIgnoreCase))
                throw new SecurityTokenException("Invalid token type");

            return principal;
        }

        public ClaimsPrincipal? ValidateTokenByType(JwtTokenType tokenType, IRequestCookieCollection cookies)
        {
            var cookieName = CookieNameByType(tokenType);
            if (!cookies.TryGetValue(cookieName, out var rawToken) || string.IsNullOrEmpty(rawToken))
                return null;

            try
            {
                return ValidateAndGetPrincipal(rawToken, tokenType);
            }
            catch
            {
                return null;
            }
        }

        private static string CookieNameByType(JwtTokenType type) => type switch
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
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }


}
