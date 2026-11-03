namespace Classified.Shared.Constants
{
    public enum JwtTokenType
    {
        None = 0,
        Access,
        Refresh,
        Restore,
        Device,
        PasswordReset,
        RequestNewEmailCofirmation,
        EmailReset,
        TwoFactorAuthentication,
        OAuthRegistration
    }

}
