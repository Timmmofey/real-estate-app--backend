using Classified.Shared.Entities;

namespace AuthService.Persistance.Entities
{
    public class SessionEntity
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public UserRoleEntity Role { get; set; }
        public Guid Token { get; set; } = default!;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }

        public Guid DeviceId { get; set; } = default!;
        public string DeviceName { get; set; } = default!;
        public string? IpAddress { get; set; } = default!;
    }
}
