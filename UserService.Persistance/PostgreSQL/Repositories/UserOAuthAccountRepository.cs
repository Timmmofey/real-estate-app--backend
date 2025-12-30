using Classified.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata.Ecma335;
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

        public async Task<ICollection<UserOAuthAccount>> GetUsersOAuthAccountsByUserId(Guid userId)
        {
            
            var accountEntities = await _context.UserOAuthAccounts.Where(account => account.UserId == userId).ToListAsync();

            var accounts = accountEntities.Select(entity => UserOAuthAccount.CreateExisting(
                entity.Id,
                entity.UserId,
                entity.OAuthProviderName,
                entity.ProviderUserId,
                entity.CreatedAt
            ))
            .Where(result => result.error == null)
            .Select(result => result.oAuthAccount!)
            .ToList();

            return accounts;
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
            if (await ChechIfUserHasOauthAccountWithSameProvider(userOAuthAccount.OAuthProviderName, userOAuthAccount.UserId) == true)
                throw new InvalidOperationException();

            var entity = new UserOAuthAccountEntity
            {
                Id = userOAuthAccount.Id,
                UserId = userOAuthAccount.UserId,
                OAuthProviderName = userOAuthAccount.OAuthProviderName,
                ProviderUserId = userOAuthAccount.ProviderUserId,
                CreatedAt = userOAuthAccount.CreatedAt
            };

            await _context.UserOAuthAccounts.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ChechIfUserHasOauthAccountWithSameProvider(OAuthProvider provider, Guid userId)
        {
            var res = await _context.UserOAuthAccounts.FirstOrDefaultAsync(a => a.OAuthProviderName == provider && a.UserId == userId);
            if (res == null) return false;
            return true;
        }

        public async Task DeleteOAuthAccountByUserIdAsync(OAuthProvider provider, Guid userId)
        {
            var deleted = await _context.UserOAuthAccounts
                .Where(a => a.OAuthProviderName == provider && a.UserId == userId)
                .ExecuteDeleteAsync();
        }

        public async Task DeleteAllOAuthAccountsByUserIdAsync(Guid userId)
        {
            var deleted = await _context.UserOAuthAccounts
                .Where(a => a.UserId == userId)
                .ExecuteDeleteAsync();
        }

    }
}
