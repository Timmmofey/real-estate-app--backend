namespace UserService.Infrastructure.AuthService
{
    public interface IAuthServiceClient
    {
        Task<string?> GetResetPasswordTokenAsync(Guid userId, CancellationToken ct);
        Task<string?> GetEmailResetTokenAsync(Guid userId, string newEmail, CancellationToken ct);
        Task<string?> GetRequestNewEmailCofirmationTokenAsync(Guid userId, CancellationToken ct);
    }
}
