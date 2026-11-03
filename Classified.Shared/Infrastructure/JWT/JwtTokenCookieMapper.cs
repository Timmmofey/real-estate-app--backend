using Classified.Shared.Constants;

namespace Classified.Shared.Infrastructure.JWT
{
    public static class JwtTokenCookieMapper
    {
        public static string? GetCookieName(JwtTokenType tokenType)
        {
            return tokenType switch
            {
                JwtTokenType.Access => CookieNames.Auth,
                JwtTokenType.Refresh => CookieNames.Refresh,
                JwtTokenType.Device => CookieNames.Device,
                JwtTokenType.PasswordReset => CookieNames.PasswordReset,
                JwtTokenType.TwoFactorAuthentication => CookieNames.TwoFactorAuthentication,
                JwtTokenType.Restore => CookieNames.Restore,
                JwtTokenType.RequestNewEmailCofirmation => CookieNames.RequestNewEmailCofirmation,
                JwtTokenType.EmailReset => CookieNames.EmailReset,
                JwtTokenType.OAuthRegistration => CookieNames.OAuthRegistration,
                _ => null
            };
        }
    }
}
