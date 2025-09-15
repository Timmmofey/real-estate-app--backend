using Classified.Shared.DTOs;

namespace AuthService.Domain.Abstactions
{
    public interface IUserServiceClient
    {
        Task<VerifiedUserDto?> VerifyUserCredentialsAsync(string phoneOrEmail, string password);
        Task<string?> GetUserBy(string userId);
    }
}
