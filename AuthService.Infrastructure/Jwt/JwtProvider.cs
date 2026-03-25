using AuthService.Domain.Abstactions;
using Classified.Shared.Constants;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AuthService.Infrastructure.Jwt
{
    public class JwtProvider : IJwtProvider
    {
        private readonly IConfiguration _config;
        public JwtProvider(IConfiguration config) => _config = config;

        public string GenerateAccessToken(Guid userId, UserRole role, Guid sessionId)
        {
            var claims = new[]
            {
                new Claim("userId", userId.ToString()),
                new Claim(ClaimTypes.Role, role.ToString()),
                new Claim("sessionId", sessionId.ToString()),
                new Claim("type", JwtTokenType.Access.ToString())
            };

            return GenerateToken(claims, DateTime.UtcNow.AddMinutes(15));
        }

        public string GenerateRefreshToken(Guid refreshtoken, string ip)
        {
            var claims = new[]
            {
                new Claim("token", refreshtoken.ToString()),
                new Claim("type", JwtTokenType.Refresh.ToString()),
                new Claim("ip", ip)
            };

            return GenerateToken(claims, DateTime.UtcNow.AddDays(150));
        }

        public string GenerateRestoreToken(Guid userId)
        {
            var claims = new[]
            {
                new Claim("userId", userId.ToString()),
                new Claim("type", JwtTokenType.Restore.ToString())
            };

            return GenerateToken(claims, DateTime.UtcNow.AddMinutes(15));
        }

        public string GenerateDeviceToken(Guid deviceId)
        {
            var claims = new[]
            {
                new Claim("deviceId", deviceId.ToString()),
                new Claim("type", JwtTokenType.Device.ToString())
            };

            return GenerateToken(claims, DateTime.UtcNow.AddDays(150));
        }

        public string GenerateResetPasswordResetToken(Guid userId)
        {
            var claims = new[]
            {
                new Claim("userId", userId.ToString()),
                new Claim("type", JwtTokenType.PasswordReset.ToString())
            };

            return GenerateToken(claims, DateTime.UtcNow.AddMinutes(15));
        }

        public string GenerateResetEmailResetToken(Guid userId, string newEmail)
        {
            var claims = new[]
            {
                new Claim("userId", userId.ToString()),
                new Claim("type", JwtTokenType.EmailReset.ToString()),
                new Claim(ClaimTypes.Email, newEmail)
            };

            return GenerateToken(claims, DateTime.UtcNow.AddMinutes(15));
        }

        public string GenerateRequestNewEmailCofirmationToken(Guid userId)
        {
            var claims = new[]
            {
                new Claim("userId", userId.ToString()),
                new Claim("type", JwtTokenType.RequestNewEmailCofirmation.ToString())
            };

            return GenerateToken(claims, DateTime.UtcNow.AddMinutes(15));
        }

        public string GenerateTwoFactorAuthToken(Guid userId)
        {
            var claims = new[]
            {
                new Claim("userId", userId.ToString()),
                new Claim("type", JwtTokenType.TwoFactorAuthentication.ToString())
            };

            return GenerateToken(claims, DateTime.UtcNow.AddMinutes(15));
        }

        public string GenerateOAuthRegistrationToken(
            string email,
            string provider,
            string providerUserId,
            string? picture)
        {
            var claims = new[]
            {
                new Claim (ClaimTypes.Email, email),
                new Claim ("provider", provider),
                new Claim ("providerUserId", providerUserId),
                new Claim ("type", JwtTokenType.OAuthRegistration.ToString())
            };

            //if (!string.IsNullOrEmpty(picture))
            //    claims.Add(new Claim("picture", picture));

            return GenerateToken(claims, DateTime.UtcNow.AddMinutes(10));
        }



        /// <summary>
        /// Private Methods
        /// </summary>



        private string GenerateToken(IEnumerable<Claim> claims, DateTime expires)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"]!)
            );

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
