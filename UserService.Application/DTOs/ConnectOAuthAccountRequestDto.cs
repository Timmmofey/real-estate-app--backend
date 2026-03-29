using Classified.Shared.Constants;

namespace UserService.Application.DTOs
{
    public record ConnectOAuthAccountRequestDto(
        OAuthProvider Provider,
        string ProviderId,
        Guid UserId
    );
}
