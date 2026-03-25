using Classified.Shared.Constants;
using UserService.Domain.Models;

namespace UserService.Domain.Abstactions
{
    public interface IUserOAuthAccountRepository{
        Task<ICollection<UserOAuthAccount>> GetUsersOAuthAccountsByUserId(Guid userId, CancellationToken ct);
        Task<UserOAuthAccount?> GetUserOAuthAccountByProviderAndProviderUserId(OAuthProvider provider, string providerUserId, CancellationToken ct);
        Task AddUserOAuthAccountAsync(UserOAuthAccount userOAuthAccount, CancellationToken ct);
        Task<bool> ChechIfUserHasOauthAccountWithSameProvider(OAuthProvider provider, Guid userId, CancellationToken ct);
        Task DeleteOAuthAccountByUserIdAsync(OAuthProvider provider, Guid userId, CancellationToken ct);
        Task DeleteAllOAuthAccountsByUserIdAsync(Guid userId, CancellationToken ct);
    }
}
