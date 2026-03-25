using Classified.Shared.Constants;
using Classified.Shared.DTOs;

namespace UserService.Application.Services
{
    public interface IUserOAuthAccountSevice
    {
        Task<UserOAuthAccountDto?> GetUserOAuthAccountByProviderAndProviderUserId(OAuthProvider provider, string providerUserId, CancellationToken ct);
        Task<ICollection<UserOAuthAccountDto>> GetUsersOAuthAccountsByUserId(Guid userId, CancellationToken ct);
        Task ConnectOauthAccountToExistingUser(OAuthProvider provider, string providerId, Guid userId, CancellationToken ct);
        Task UnLinkOAuthAccountAsync(OAuthProvider provider, Guid userId, CancellationToken ct);
    }
}
