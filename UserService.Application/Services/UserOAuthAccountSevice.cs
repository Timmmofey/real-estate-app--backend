using Classified.Shared.Constants;
using Classified.Shared.DTOs;
using Classified.Shared.Extensions.ErrorHandler;
using Classified.Shared.Extensions.ErrorHandler.Errors;
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

        public async Task<UserOAuthAccountDto?> GetUserOAuthAccountByProviderAndProviderUserId(OAuthProvider provider, string providerUserId, CancellationToken ct)
        {
            var userOAuthAccount = await _oAuthRepository.GetUserOAuthAccountByProviderAndProviderUserId(provider, providerUserId, ct);

            if (userOAuthAccount == null)
            {
                throw new ArgumentException("Provided credentials are not valid.");
            }

            return new UserOAuthAccountDto
            {
                Id = userOAuthAccount.Id,
                UserId = userOAuthAccount.UserId,
                OAuthProviderName = userOAuthAccount.OAuthProviderName.ToString(),
            };
        }

        public async Task<ICollection<UserOAuthAccountDto>> GetUsersOAuthAccountsByUserId(Guid userId, CancellationToken ct)
        {
            var accounts = await _oAuthRepository.GetUsersOAuthAccountsByUserId(userId, ct);

            var dtos = accounts.Select(account => new UserOAuthAccountDto
            {
                Id = account.Id,
                UserId = account.UserId,
                OAuthProviderName = account.OAuthProviderName.ToString()
            }).ToList();

            return dtos;
        }

        public async Task ConnectOauthAccountToExistingUser(OAuthProvider provider, string providerId, Guid userId, CancellationToken ct)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException(nameof(userId));

            if (string.IsNullOrWhiteSpace(providerId))
                throw new ArgumentException(nameof(providerId));

            if (await _oAuthRepository.ChechIfUserHasOauthAccountWithSameProvider(provider, userId, ct))
                throw new DomainValidationException("User alredy has OAuth account connected with this provider.");

            var (oAuthAccount, oAuthAccountError) = UserOAuthAccount.CreateNew(
                userId,
                provider,
                providerId
            );

            if (oAuthAccount == null)
                throw new Exception(oAuthAccountError);

            await _oAuthRepository.AddUserOAuthAccountAsync(oAuthAccount, ct);
        }

        public async Task UnLinkOAuthAccountAsync(OAuthProvider provider, Guid userId, CancellationToken ct)
        {
            var user = await _userRepository.GetUserById(userId, ct)
                ?? throw new DomainValidationException("User doesn`t exust.");

            var accounts = await _oAuthRepository.GetUsersOAuthAccountsByUserId(userId, ct);

            var accountToDelete = accounts
                .FirstOrDefault(a => a.OAuthProviderName == provider)
                ?? throw new DomainValidationException("OAuth account has not been found");

            var hasPassword = !string.IsNullOrEmpty(user.PasswordHash);
            var hasOtherAuthMethods = hasPassword || accounts.Count > 1;

            if (!hasOtherAuthMethods)
                throw new DomainValidationException("User have only one auth method.");

            await _oAuthRepository.DeleteOAuthAccountByUserIdAsync(provider, userId, ct);
        }


    }
}
