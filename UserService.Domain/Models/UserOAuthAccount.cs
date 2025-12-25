using Classified.Shared.Constants;

namespace UserService.Domain.Models
{
    public class UserOAuthAccount
    {
        public Guid Id { get; }
        public Guid UserId { get; }
        public OAuthProvider OAuthProviderName { get; }
        public string ProviderUserId { get; } = default!;
        public DateTime CreatedAt { get; } = DateTime.UtcNow;

        private UserOAuthAccount(Guid id, Guid userId, OAuthProvider provider, string providerUserId, DateTime createdAt)
        {
            Id = id;
            UserId = userId;
            OAuthProviderName = provider;
            ProviderUserId = providerUserId;
            CreatedAt = createdAt;
        }

        private static string? ValidateInputs(Guid userId, OAuthProvider provider, string providerUserId)
        {
            if (userId == Guid.Empty)
                return "UserId cannot be empty";

            if (string.IsNullOrWhiteSpace(providerUserId))
                return "ProviderUserId is required";

            if (!Enum.IsDefined(typeof(OAuthProvider), provider))
                return "Invalid OAuthProvider value";

            return null;
        }


        public static (UserOAuthAccount? oAuthAccount, string? error) CreateNew(Guid userId, OAuthProvider provider, string providerUserId)
        {
            var error = ValidateInputs(userId, provider, providerUserId);
            if (error != null) return (null, error);

            var account = new UserOAuthAccount(Guid.NewGuid(), userId, provider, providerUserId, DateTime.UtcNow);
            return (account, null);
        }

        public static (UserOAuthAccount? oAuthAccount, string? error) CreateExisting(Guid id, Guid userId, OAuthProvider provider, string providerUserId, DateTime createdAt)
        {
            var error = ValidateInputs(userId, provider, providerUserId);
            if (error != null) return (null, error);

            if (id == Guid.Empty) return (null, "Id cannot be empty");

            var account = new UserOAuthAccount(id, userId, provider, providerUserId, createdAt);
            return (account, null);
        }

    }
}
