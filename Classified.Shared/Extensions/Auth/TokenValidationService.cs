using Classified.Shared.Constants;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Classified.Shared.Extensions.Auth
{
    public interface ITokenValidationService
    {
        ClaimsPrincipal ValidateAndGetPrincipal(string token, JwtTokenType expectedType);
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

            if (validatedToken is not JwtSecurityToken jwtToken)
                throw new SecurityTokenException("Invalid token");

            if (!jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.OrdinalIgnoreCase))
                throw new SecurityTokenException("Invalid signing algorithm");

            if (jwtToken.ValidTo < DateTime.UtcNow)
                throw new SecurityTokenExpiredException("Token expired");

            var typeClaim = principal.FindFirst("type")?.Value;
            if (typeClaim == null)
                throw new SecurityTokenException("Missing token type");

            if (!string.Equals(typeClaim, expectedType.ToString(), StringComparison.OrdinalIgnoreCase))
                throw new SecurityTokenException("Invalid token type");

            return principal;
        }

    }


}
