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

        public async Task AddNewRefreshTokenAsync(Session rt)
        {
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

            await _context.Sessions.AddAsync(sessionEntity);
            await _context.SaveChangesAsync();
        }

        public async Task AddOrUpdateRefreshTokenAsync(Session rt)
        {
            var existingEntity = await _context.Sessions
                .FirstOrDefaultAsync(srt => srt.DeviceId == rt.DeviceId);

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
                // Создаём новую запись
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

                await _context.Sessions.AddAsync(newEntity);
            }

            await _context.SaveChangesAsync();
        }


        public async Task<bool> DeleteRefreshTokenByUserIdAndIdAsync(Guid userId, Guid id)
        {
            var deletedCount = await _context.Sessions
                .Where(rt => rt.UserId == userId && rt.Id == id)
                .ExecuteDeleteAsync();

            return deletedCount > 0;
        }


        public async Task DeleteRefreshTokenByUserIdAndDeviceIDAsync(Guid deviceId)
        {
            await _context.Sessions.Where(rt => rt.DeviceId == deviceId).ExecuteDeleteAsync();
        }

        public async Task DeleteAllRefreskTokensByUserId(Guid userId)
        {
            await _context.Sessions.Where(rt => rt.UserId == userId).ExecuteDeleteAsync();
        }

        public async Task<ICollection<SessionEntity>> FindRefreshTokensByUserId(Guid userId)
        {
            return await _context.Sessions.Where(rt => rt.UserId == userId).ToListAsync();
        }

        public async Task<Session> FindSessionByRefreshTokenAndDeviceId(Guid refreshToken,Guid deviceId)
        {
            var refreshTokenEntity = await _context.Sessions.FirstOrDefaultAsync(s => s.DeviceId == deviceId && s.Token == refreshToken);

            if (refreshTokenEntity == null)
            {
                throw new Exception($"User Refresh Token has not been found");
            }

            var (session, error) = Session.Create(refreshTokenEntity.Id, refreshTokenEntity.UserId, (UserRole)refreshTokenEntity.Role, refreshTokenEntity.Token, refreshTokenEntity.DeviceId, refreshTokenEntity.DeviceName, refreshTokenEntity.DeviceType, refreshTokenEntity.IpAddress, refreshTokenEntity.Country, refreshTokenEntity.Settlemnet, refreshTokenEntity.CreatedAt, refreshTokenEntity.ExpiresAt);

            if (session == null)
            {
                throw new Exception($"User Refresh Token is invalid: {error}");
            }

            return session;
        }

        public async Task<ICollection<Session>> GetUsersSessionsAsync(Guid userId)
        {
            var refreshTokensEnities = await _context.Sessions.Where(rt => rt.UserId == userId).ToListAsync();
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
