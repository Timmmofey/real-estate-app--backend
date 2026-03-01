using AuthService.Domain.Models;
using Classified.Shared.Constants;

namespace AuthService.Domain.Abstactions
{
    public interface IRefreshTokenRepository
    {
        Task AddNewRefreshTokenAsync(Session rt);
        Task AddOrUpdateRefreshTokenAsync(Session rt);
        Task<Session?> UpdateAndRotateRefreshTokenAsync(Guid oldRt, Guid deviceId, Guid newRefreshToken, string? deviceName = null, DeviceType? deviceType = null, string? ipAddress = null, string? country = null, string? city = null);
        Task<bool> DeleteRefreshTokenByUserIdAndIdAsync(Guid userId, Guid id);
        Task DeleteRefreshTokenByUserIdAndDeviceIDAsync(Guid deviceId);
        Task DeleteAllRefreskTokensByUserId(Guid userId);
        Task<Session> FindSessionByRefreshTokenAndDeviceId(Guid refreshToken, Guid deviceId);
        Task<ICollection<Session>> GetUsersSessionsAsync(Guid userId);
    }
}
