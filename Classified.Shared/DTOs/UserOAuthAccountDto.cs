using Classified.Shared.Constants;

namespace Classified.Shared.DTOs
{
    public class UserOAuthAccountDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string OAuthProviderName { get; set; } = default!;
        //public string ProviderUserId { get; set; } = default!;
        //public DateTime CreatedAt { get; set; }
    }
}
