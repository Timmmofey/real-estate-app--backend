using Classified.Shared.Constants;
using Classified.Shared.DTOs;
using Microsoft.EntityFrameworkCore;
using UserService.Application.Exeptions;
using UserService.Domain.Abstactions;
using UserService.Domain.Models;

namespace UserService.Application.Services
{
    public class UserOAuthAccountSevice: IUserOAuthAccountSevice
    {
        private readonly IUserOAuthAccountRepository _oAuthRepository;

        public UserOAuthAccountSevice(IUserOAuthAccountRepository oAuthRepository)
        {
            _oAuthRepository = oAuthRepository;
        }

        public async Task<UserOAuthAccountDto?> GetUserOAuthAccountByProviderAndProviderUserId(OAuthProvider provider, string providerUserId)
        {
            var userOAuthAccount = await _oAuthRepository.GetUserOAuthAccountByProviderAndProviderUserId(provider, providerUserId);

            if (userOAuthAccount == null)
            {
                throw new NotValidCredentialsException();
            }

            return new UserOAuthAccountDto
            {
                Id = userOAuthAccount.Id,
                UserId = userOAuthAccount.UserId,
                ProviderUserId = userOAuthAccount.ProviderUserId,
                OAuthProviderName = userOAuthAccount.OAuthProviderName,
                CreatedAt = userOAuthAccount.CreatedAt,
            };
        }
    }
}
