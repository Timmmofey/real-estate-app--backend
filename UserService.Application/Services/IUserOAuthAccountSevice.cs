using Classified.Shared.Constants;
using Classified.Shared.DTOs;
using UserService.Domain.Models;

namespace UserService.Application.Services
{
    public interface IUserOAuthAccountSevice
    {
        Task<UserOAuthAccountDto?> GetUserOAuthAccountByProviderAndProviderUserId(OAuthProvider provider, string providerUserId);
    }
}
