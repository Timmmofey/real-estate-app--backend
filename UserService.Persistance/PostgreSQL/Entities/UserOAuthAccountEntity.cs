using Classified.Shared.Constants;

namespace UserService.Persistance.PostgreSQL.Entities
{
    public class UserOAuthAccountEntity
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public OAuthProvider OAuthProviderName { get; set; }
        public required string ProviderUserId { get; set; } 
        public DateTime CreatedAt { get; set; }
        public UserEntity User { get; set; } = default!;
    }
}
