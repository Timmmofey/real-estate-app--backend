using AuthService.Domain.DTOs;

namespace AuthService.Domain.Abstactions
{
    public interface IUserServiceClient
    {
        Task<VerifiedUserDto?> VerifyUserCredentialsAsync(string phoneOrEmail, string password);
    }
}
