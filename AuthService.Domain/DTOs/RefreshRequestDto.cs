namespace AuthService.Domain.DTOs
{
    public class RefreshRequestDto
    {
        public string RefreshToken { get; set; } = default!;
        public string DeviceId { get; set; } = default!;
    }
}