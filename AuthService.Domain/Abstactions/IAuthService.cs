using AuthService.Domain.DTOs;

namespace AuthService.Domain.Abstactions
{
    public interface IAuthService
    {
        Task<(TokenResponseDto?, string?)> LoginAsync(string phoneOrEmail, string password, Guid deviceId);
        Task<TokenResponseDto> RefreshAsync(Guid refreshToken, Guid deviceId);
        Task LogoutAync(Guid deviceId);
        Task LogoutAllAsync(Guid userId);
        Task<bool> TerminateSession(Guid userId, Guid id);
        Task<ICollection<SessionDto>> GetUsersSessions(Guid userId);
    }
}
