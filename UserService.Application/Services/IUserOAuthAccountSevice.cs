using Classified.Shared.Constants;
using Classified.Shared.DTOs;

namespace UserService.Application.Services
{
    public interface IUserOAuthAccountSevice
    {
        Task<UserOAuthAccountDto?> GetUserOAuthAccountByProviderAndProviderUserId(OAuthProvider provider, string providerUserId);
        Task<ICollection<UserOAuthAccountDto>> GetUsersOAuthAccountsByUserId(Guid userId);
        Task ConnectOauthAccountToExistingUser(OAuthProvider provider, string providerId, Guid userId);
        Task UnLinkOAuthAccountAsync(OAuthProvider provider, Guid userId);
    }
}
