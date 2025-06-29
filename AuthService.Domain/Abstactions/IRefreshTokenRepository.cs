using AuthService.Domain.Models;

namespace AuthService.Domain.Abstactions
{
    public interface IRefreshTokenRepository
    {
        Task AddNewRefreshTokenAsync(Session rt);
        Task AddOrUpdateRefreshTokenAsync(Session rt);
        Task<bool> DeleteRefreshTokenByUserIdAndIdAsync(Guid userId, Guid id);
        Task DeleteRefreshTokenByUserIdAndDeviceIDAsync(Guid deviceId);
        Task DeleteAllRefreskTokensByUserId(Guid userId);
        Task<Session> FindSessionByRefreshTokenAndDeviceId(Guid refreshToken, Guid deviceId);
        Task<ICollection<Session>> GetUsersSessionsAsync(Guid userId);
    }
}
