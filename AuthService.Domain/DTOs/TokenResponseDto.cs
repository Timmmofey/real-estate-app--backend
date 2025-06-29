namespace AuthService.Domain.DTOs
{
    public class TokenResponseDto
    {
        public string AccessToken { get; set; } = default!;
        public string RefreshToken { get; set; } = default!;
        public string DeviceToken { get; set; } = default!;
    }
}