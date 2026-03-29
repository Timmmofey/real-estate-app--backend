//using AuthService.Domain.Models;
//using Classified.Shared.Constants;

//namespace AuthService.Domain.Abstactions
//{
//    public interface IRefreshTokenRepository
//    {
//        Task AddNewRefreshTokenAsync(Session rt, CancellationToken ct);
//        Task AddOrUpdateRefreshTokenAsync(Session rt, CancellationToken ct);
//        Task<Session?> UpdateAndRotateRefreshTokenAsync(Guid oldRt, Guid deviceId, Guid newRefreshToken, CancellationToken ct, string? deviceName = null, DeviceType? deviceType = null, string? ipAddress = null, string? country = null, string? city = null);
//        Task<bool> DeleteRefreshTokenByUserIdAndIdAsync(Guid userId, Guid id, CancellationToken ct);
//        Task DeleteRefreshTokenByUserIdAndDeviceIDAsync(Guid deviceId, CancellationToken ct);
//        Task DeleteAllRefreskTokensByUserId(Guid userId, CancellationToken ct);
//        Task<Session> FindSessionByRefreshTokenAndDeviceId(Guid refreshToken, Guid deviceId, CancellationToken ct);
//        Task<ICollection<Session>> GetUsersSessionsAsync(Guid userId, CancellationToken ct);
//    }
//}
