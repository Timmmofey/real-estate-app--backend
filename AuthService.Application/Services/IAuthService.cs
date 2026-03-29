using AuthService.Domain.DTOs;
using Classified.Shared.Constants;
using Classified.Shared.DTOs;

namespace AuthService.Domain.Abstactions
{
    public interface IAuthService
    {
        Task<(TokenResponseDto?, string?, string?)> LoginAsync(string phoneOrEmail, string password, Guid deviceId, CancellationToken ct);
        Task<TokenResponseDto?> LoginViaTWoFactorAuthentication(string userId, string deviceId, string code, CancellationToken ct);
        Task<(
            TokenResponseDto? tokens,
            string? restoreToken,
            string? twoFactorAuthToken
        )> LoginWithOAuthAsync(Guid userId, Guid deviceId, CancellationToken ct);
        Task<string?> GetUserIdByEmailAsync(string email, CancellationToken ct);
        Task<UserOAuthAccountDto?> GetUserOAuthAccountByProviderAndProviderUserIdAsync(OAuthProvider provider, string providerUserId, CancellationToken ct);
        Task LinkOAuthAccountAsync(OAuthProvider provider, string providerId, Guid userId, CancellationToken ct);
        //Task<TokenResponseDto> RefreshAsync(Guid refreshToken, Guid deviceId);
        Task<TokenResponseDto> RefreshAndRorateAsync(Guid refreshToken, Guid deviceId, string prevIp, CancellationToken ct);
        Task LogoutAync(Guid deviceId, CancellationToken ct);
        Task LogoutAllAsync(Guid userId, CancellationToken ct);
        Task<bool> TerminateSession(Guid userId, Guid id, CancellationToken ct);
        Task<ICollection<SessionResponseDto>> GetUsersSessions(Guid userId, Guid sessionId, CancellationToken ct);
    }
}
