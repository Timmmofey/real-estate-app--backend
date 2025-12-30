using Classified.Shared.Constants;

namespace UserService.Application.DTOs
{
    public class ConnectOAuthAccountRequest
    {
        public OAuthProvider Provider { get; set; }
        public string ProviderId { get; set; } = default!;
        public Guid UserId { get; set; }
    };
}
