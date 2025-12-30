using Classified.Shared.Constants;
using Classified.Shared.DTOs;
using System.Threading;
using UserService.Application.Exeptions;
using UserService.Domain.Abstactions;
using UserService.Domain.Models;

namespace UserService.Application.Services
{
    public class UserOAuthAccountSevice: IUserOAuthAccountSevice
    {
        private readonly IUserOAuthAccountRepository _oAuthRepository;
        private readonly IUserRepository _userRepository;

        public UserOAuthAccountSevice(IUserOAuthAccountRepository oAuthRepository, IUserRepository userRepository)
        {
            _oAuthRepository = oAuthRepository;
            _userRepository = userRepository;
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
                OAuthProviderName = userOAuthAccount.OAuthProviderName.ToString(),
            };
        }

        public async Task<ICollection<UserOAuthAccountDto>> GetUsersOAuthAccountsByUserId(Guid userId)
        {
            var accounts = await _oAuthRepository.GetUsersOAuthAccountsByUserId(userId);

            var dtos = accounts.Select(account => new UserOAuthAccountDto
            {
                Id = account.Id,
                UserId = account.UserId,
                OAuthProviderName = account.OAuthProviderName.ToString()
            }).ToList();

            return dtos;
        }

        public async Task ConnectOauthAccountToExistingUser(OAuthProvider provider, string providerId, Guid userId)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException(nameof(userId));

            if (string.IsNullOrWhiteSpace(providerId))
                throw new ArgumentException(nameof(providerId));

            if (await _oAuthRepository.ChechIfUserHasOauthAccountWithSameProvider(provider, userId))
                throw new OAuthAccountAlreadyLinkedException();


            var (oAuthAccount, oAuthAccountError) = UserOAuthAccount.CreateNew(
                userId,
                provider,
                providerId
            );

            if (oAuthAccount == null)
                throw new Exception(oAuthAccountError);

            await _oAuthRepository.AddUserOAuthAccountAsync(oAuthAccount);
        }

        public async Task UnLinkOAuthAccountAsync(OAuthProvider provider, Guid userId)
        {
            var user = await _userRepository.GetUserById(userId)
                ?? throw new UserNotFoundException();

            var accounts = await _oAuthRepository.GetUsersOAuthAccountsByUserId(userId);

            var accountToDelete = accounts
                .FirstOrDefault(a => a.OAuthProviderName == provider)
                ?? throw new OAuthAccountNotFoundException();

            var hasPassword = !string.IsNullOrEmpty(user.PasswordHash);
            var hasOtherAuthMethods = hasPassword || accounts.Count > 1;

            if (!hasOtherAuthMethods)
                throw new CannotUnlinkLastAuthMethodException();

            await _oAuthRepository.DeleteOAuthAccountByUserIdAsync(provider, userId);
        }


    }
}
