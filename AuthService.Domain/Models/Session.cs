using Classified.Shared.Constants;

namespace AuthService.Domain.Models
{
    public class Session
    {
        public Guid Id { get; }
        public Guid UserId { get; }
        public UserRole Role { get; }
        public Guid Token { get; } = default!;
        public DateTime CreatedAt { get; }
        public DateTime ExpiresAt { get; }

        public Guid DeviceId { get; } = default!;
        public string DeviceName { get; } = default!;
        public string? IpAddress { get; } = default!;

        private Session(Guid id, Guid userId, UserRole role, Guid token,  Guid deviceId, string deviceName, string? ipAddress, DateTime? createdAt, DateTime? expiresAt) 
        {
            Id = id;
            UserId = userId;
            Role = role;
            Token = token;
            CreatedAt = createdAt ?? DateTime.UtcNow;
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddDays(15);
            DeviceId = deviceId;
            DeviceName = deviceName;
            IpAddress = ipAddress ?? null;
        }

        private static string? ValidateRefreshToken(Guid id, Guid userId, Guid token, Guid deviceId, string deviceName, string? ipAddress)
        {
            if (id == Guid.Empty)
                return "Id cannot be empty.";
            if (userId == Guid.Empty)
                return "User Id cannot be empty.";
            if (token == Guid.Empty)
                return "Token Hash cannot be empty.";
            if (string.IsNullOrWhiteSpace(deviceName))
                return "Device Name cannot be empty.";
            if (string.IsNullOrWhiteSpace(ipAddress))
                return "Ip Address cannot be empty.";
            return null;
        }

        public static (Session? SessionToken, string? Error) Create(Guid id, Guid userId, UserRole role, Guid token, Guid deviceId, string deviceName, string? ipAddress, DateTime? CreatedAt, DateTime? ExpiresAt)
        {
            var validationError = ValidateRefreshToken(id, userId, token, deviceId, deviceName, ipAddress);
            if (validationError != null) return (null, validationError);

            var refreshToken = new Session(id, userId, role, token, deviceId, deviceName, ipAddress, CreatedAt, ExpiresAt);

            return (refreshToken, null);
        }
    }

}
