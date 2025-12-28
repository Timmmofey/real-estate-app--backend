using Classified.Shared.Constants;
using UserService.Domain.Models;

namespace UserService.Domain.Abstactions
{
    public interface IUserOAuthAccountRepository{
        Task<UserOAuthAccount?> GetUserOAuthAccountByProviderAndProviderUserId(OAuthProvider provider, string providerUserId);
        Task AddUserOAuthAccountAsync(UserOAuthAccount userOAuthAccount);
    }
}
