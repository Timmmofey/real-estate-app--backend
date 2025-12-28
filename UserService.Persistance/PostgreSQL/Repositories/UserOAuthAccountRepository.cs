using Classified.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using UserService.Domain.Abstactions;
using UserService.Domain.Models;
using UserService.Persistance.PostgreSQL.Entities;

namespace UserService.Persistance.PostgreSQL.Repositories
{
    public class UserOAuthAccountRepository: IUserOAuthAccountRepository
    {
        private readonly UserServicePostgreDbContext _context;

        public UserOAuthAccountRepository(UserServicePostgreDbContext context)
        {
            _context = context;
        }

        public async Task<UserOAuthAccount?> GetUserOAuthAccountByProviderAndProviderUserId(OAuthProvider provider, string providerUserId)
        {
            var userOAuthAccountEntity = await _context.UserOAuthAccounts.FirstOrDefaultAsync(profile => profile.OAuthProviderName == provider && profile.ProviderUserId == providerUserId);

            if (userOAuthAccountEntity == null) 
                return null;

            var (userOAuthAccount, error) = UserOAuthAccount.CreateExisting(
                userOAuthAccountEntity.Id,
                userOAuthAccountEntity.UserId,
                userOAuthAccountEntity.OAuthProviderName,
                userOAuthAccountEntity.ProviderUserId,
                userOAuthAccountEntity.CreatedAt
            );

            if (error != null)
                throw new InvalidOperationException(error);

            return userOAuthAccount;
        }

        public async Task AddUserOAuthAccountAsync(UserOAuthAccount userOAuthAccount)
        {
            var entity = new UserOAuthAccountEntity
            {
                Id = userOAuthAccount.Id,
                UserId = userOAuthAccount.UserId,
                OAuthProviderName = userOAuthAccount.OAuthProviderName,
                ProviderUserId = userOAuthAccount.ProviderUserId,
                CreatedAt = userOAuthAccount.CreatedAt
            };

            await _context.UserOAuthAccounts.AddAsync(entity);
        }

    }
}
