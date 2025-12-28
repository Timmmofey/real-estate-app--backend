namespace AuthService.Domain.DTOs
{
    public class GoogleUserDto
    {
        public string Sub { get; set; } = default!;
        public string Email { get; set; } = default!;
        public bool EmailVerified { get; set; }
        public string? Name { get; set; }
        public string? Picture { get; set; }
    }
}