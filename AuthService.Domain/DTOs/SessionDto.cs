namespace AuthService.Domain.DTOs
{
    public class SessionDto
    {
        public Guid SessionId { get; set; } = default!;
        public string DeviceName { get; set; } = default!;
        public string? IpAddress { get; set; } = default!;
        //public DateTime CreatedAt { get; set; }
        //public DateTime ExpiresAt { get; set; }
        //public bool IsRevoked => RevokedAt != null;
        //public DateTime? RevokedAt { get; set; }
    }
}