using Classified.Shared.Constants;

namespace Classified.Shared.DTOs
{
    public class UserOAuthAccountDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public OAuthProvider OAuthProviderName { get; set; }
        public string ProviderUserId { get; set; } = default!;
        public DateTime CreatedAt { get; set; }
    }
}
