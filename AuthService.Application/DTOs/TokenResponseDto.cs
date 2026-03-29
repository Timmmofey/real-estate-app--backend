namespace AuthService.Domain.DTOs
{
    public record TokenResponseDto
    {
        public required string AccessToken { get; set; }
        public required string RefreshToken { get; set; }
        public required string DeviceToken { get; set; }
    }
}