using Classified.Shared.Constants;
using Classified.Shared.DTOs;

namespace AuthService.Domain.Abstactions
{
    public interface IUserServiceClient
    {
        Task<VerifiedUserDto?> VerifyUserCredentialsAsync(string phoneOrEmail, string password);
        Task<VerifiedUserDto?> GetVerifiedUserDtoByIdAsync(string userId);
        Task<UserOAuthAccountDto?> GetUserOAuthAccountByProviderAndProviderUserIdAsync(OAuthProvider provider, string providerUserId);
        Task<string?> GetUserIdByEmailAsync(string email);
    }
}
