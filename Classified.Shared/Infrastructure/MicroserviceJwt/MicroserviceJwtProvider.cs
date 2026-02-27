using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Classified.Shared.Infrastructure.MicroserviceJwt
{
    public class MicroserviceJwtProvider : IMicroserviceJwtProvider
    {
        private readonly IConfiguration _config;

        public MicroserviceJwtProvider(IConfiguration config)
        {
            _config = config;
        }

        public string GenerateToken(string subject, string audience, int expiresMinutes = 1)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, subject)
            };

            var issuer = _config["ServerJwt:Issuer"]
               ?? throw new InvalidOperationException("ServerJwt:Issuer is not configured");

            var key = _config["ServerJwt:ServerKey"]
                ?? throw new InvalidOperationException("ServerJwt:ServerKey is not configured");

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,     
                audience: audience, 
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
