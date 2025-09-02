namespace UserService.Infrastructure.AuthService
{
    public interface IAuthServiceClient
    {
        Task<string?> getResetPasswordToken(Guid userId);
        Task<string?> getEmailResetToken(Guid userId, string newEmail);
        Task<string?> getRequestNewEmailCofirmationToken(Guid userId);
    }
}
