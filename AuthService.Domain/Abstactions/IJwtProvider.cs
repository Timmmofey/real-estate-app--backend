using Classified.Shared.Constants;

namespace AuthService.Domain.Abstactions
{
    public interface IJwtProvider
    {
        string GenerateAccessToken(Guid userId, UserRole role, Guid sessionId);
        string GenerateRefreshToken(Guid refreshtoken);
        string GenerateRestoreToken(Guid Id);
        public string GenerateDeviceToken(Guid deviceId);
        public string GenerateResetPasswordResetToken(Guid userId);
        public string GenerateResetEmailResetToken(Guid userId, string newEmail);
        public string GenerateRequestNewEmailCofirmationToken(Guid userId);
        public string GenerateTwoFactorAuthToken(Guid userId);

    }
}
