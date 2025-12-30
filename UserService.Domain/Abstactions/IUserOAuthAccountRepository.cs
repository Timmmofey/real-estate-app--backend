using Classified.Shared.Constants;
using UserService.Domain.Models;

namespace UserService.Domain.Abstactions
{
    public interface IUserOAuthAccountRepository{
        Task<ICollection<UserOAuthAccount>> GetUsersOAuthAccountsByUserId(Guid userId);
        Task<UserOAuthAccount?> GetUserOAuthAccountByProviderAndProviderUserId(OAuthProvider provider, string providerUserId);
        Task AddUserOAuthAccountAsync(UserOAuthAccount userOAuthAccount);
        Task<bool> ChechIfUserHasOauthAccountWithSameProvider(OAuthProvider provider, Guid userId);
        Task DeleteOAuthAccountByUserIdAsync(OAuthProvider provider, Guid userId);
    }
}
