using AuthService.Domain.Abstactions;
using AuthService.Domain.Models;
using AuthService.Persistance.Entities;
using Classified.Shared.Constants;
using Classified.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Persistance.Repositories
{
    public class SessionRepository : IRefreshTokenRepository
    {
        private readonly AuthServicePostgreDbContext _context;

        public SessionRepository(AuthServicePostgreDbContext context)
        {
            _context = context;
        }

        public async Task AddNewRefreshTokenAsync(Session rt, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var sessionEntity = new SessionEntity
            {
                Id = rt.Id,
                UserId = rt.UserId,
                Role = (UserRoleEntity)rt.Role,
                Token = rt.Token,
                CreatedAt = rt.CreatedAt,
                ExpiresAt = rt.ExpiresAt,
                DeviceId = rt.DeviceId,
                DeviceName = rt.DeviceName,
                IpAddress = rt.IpAddress,
                DeviceType = rt.DeviceType,
                Country = rt.Country,
                Settlemnet = rt.Settlemnet,
            };

            await _context.Sessions.AddAsync(sessionEntity, ct);
            await _context.SaveChangesAsync(ct);
        }

        public async Task AddOrUpdateRefreshTokenAsync(Session rt, CancellationToken ct)
        {
            var existingEntity = await _context.Sessions
                .FirstOrDefaultAsync(srt => srt.DeviceId == rt.DeviceId && srt.UserId == rt.UserId, ct);

            if (existingEntity != null)
            {
                existingEntity.Token = rt.Token;
                existingEntity.ExpiresAt = rt.ExpiresAt;
                existingEntity.CreatedAt = rt.CreatedAt;
                existingEntity.DeviceName = rt.DeviceName;
                existingEntity.IpAddress = rt.IpAddress;
                existingEntity.DeviceType = rt.DeviceType;
                existingEntity.Role = (UserRoleEntity)rt.Role;
                existingEntity.UserId = rt.UserId;
                existingEntity.Country = rt.Country;
                existingEntity.Settlemnet = rt.Settlemnet;
            }
            else
            {
                var newEntity = new SessionEntity
                {
                    Id = rt.Id,
                    UserId = rt.UserId,
                    Role = (UserRoleEntity)rt.Role,
                    Token = rt.Token,
                    CreatedAt = rt.CreatedAt,
                    ExpiresAt = rt.ExpiresAt,
                    DeviceId = rt.DeviceId,
                    DeviceName = rt.DeviceName,
                    IpAddress = rt.IpAddress,
                    DeviceType = rt.DeviceType,
                    Country = rt.Country,
                    Settlemnet = rt.Settlemnet,
                };

                await _context.Sessions.AddAsync(newEntity, ct);
            }

            await _context.SaveChangesAsync(ct);
        }

        public async Task<Session?> UpdateAndRotateRefreshTokenAsync(Guid oldRt, Guid deviceId, Guid newRefreshToken, CancellationToken ct, string? deviceName = null, DeviceType? deviceType = null, string? ipAddress = null, string? country = null, string? city = null)
        {
            var existingEntity = await _context.Sessions
                .FirstOrDefaultAsync(srt => srt.DeviceId == deviceId && srt.Token == oldRt, ct);

            if (existingEntity == null)
                throw new InvalidOperationException($"User Refresh Token has not been found");

            existingEntity.Token = newRefreshToken;
            existingEntity.CreatedAt = DateTime.UtcNow;
            existingEntity.ExpiresAt = DateTime.UtcNow.AddDays(15);

            if(deviceName != null)
                existingEntity.DeviceName = deviceName;

            if (ipAddress != null)
                existingEntity.IpAddress = ipAddress;

            if (deviceType != null)
                existingEntity.DeviceType = deviceType;

            if(country != null)
                existingEntity.Country = country;

            if(city != null)    
                existingEntity.Settlemnet = city;

            await _context.SaveChangesAsync(ct);

            var (session, error) = Session.Create(existingEntity.Id, existingEntity.UserId, (UserRole)existingEntity.Role, existingEntity.Token, existingEntity.DeviceId, existingEntity.DeviceName, existingEntity.DeviceType, existingEntity.IpAddress, existingEntity.Country, existingEntity.Settlemnet, existingEntity.CreatedAt, existingEntity.ExpiresAt);

            if (session == null)
            {
                throw new InvalidOperationException($"User Refresh Token is invalid: {error}");
            }

            return session;
        }


        public async Task<bool> DeleteRefreshTokenByUserIdAndIdAsync(Guid userId, Guid id, CancellationToken ct)
        {
            var deletedCount = await _context.Sessions
                .Where(rt => rt.UserId == userId && rt.Id == id)
                .ExecuteDeleteAsync(ct);

            return deletedCount > 0;
        }


        public async Task DeleteRefreshTokenByUserIdAndDeviceIDAsync(Guid deviceId, CancellationToken ct)
        {
            await _context.Sessions.Where(rt => rt.DeviceId == deviceId).ExecuteDeleteAsync(ct);
        }

        public async Task DeleteAllRefreskTokensByUserId(Guid userId, CancellationToken ct)
        {
            await _context.Sessions.Where(rt => rt.UserId == userId).ExecuteDeleteAsync(ct);
        }

        public async Task<ICollection<SessionEntity>> FindRefreshTokensByUserId(Guid userId, CancellationToken ct)
        {
            return await _context.Sessions.Where(rt => rt.UserId == userId).ToListAsync(ct);
        }

        public async Task<Session> FindSessionByRefreshTokenAndDeviceId(Guid refreshToken,Guid deviceId, CancellationToken ct)
        {
            var refreshTokenEntity = await _context.Sessions.FirstOrDefaultAsync(s => s.DeviceId == deviceId && s.Token == refreshToken, ct);

            if (refreshTokenEntity == null)
            {
                throw new InvalidOperationException($"User Refresh Token has not been found");
            }

            var (session, error) = Session.Create(refreshTokenEntity.Id, refreshTokenEntity.UserId, (UserRole)refreshTokenEntity.Role, refreshTokenEntity.Token, refreshTokenEntity.DeviceId, refreshTokenEntity.DeviceName, refreshTokenEntity.DeviceType, refreshTokenEntity.IpAddress, refreshTokenEntity.Country, refreshTokenEntity.Settlemnet, refreshTokenEntity.CreatedAt, refreshTokenEntity.ExpiresAt);

            if (session == null)
            {
                throw new InvalidOperationException($"User Refresh Token is invalid: {error}");
            }

            return session;
        }

        public async Task<ICollection<Session>> GetUsersSessionsAsync(Guid userId, CancellationToken ct)
        {
            var refreshTokensEnities = await _context.Sessions.Where(rt => rt.UserId == userId).ToListAsync(ct);
            var refreshTokens = new List<Session>();
            foreach (var entity in refreshTokensEnities)
            {
                var (refreshToken, error) = Session.Create(
                       entity.Id,
                       entity.UserId,
                       (UserRole)entity.Role,
                       entity.Token,
                       entity.DeviceId,
                       entity.DeviceName,
                       entity.DeviceType,
                       entity.IpAddress,
                       entity.Country,
                       entity.Settlemnet,
                       entity.CreatedAt,
                       entity.ExpiresAt
                );
                if (refreshToken != null)
                {
                    refreshTokens.Add(refreshToken);
                }
            }

            return refreshTokens;
        }
    }
}
