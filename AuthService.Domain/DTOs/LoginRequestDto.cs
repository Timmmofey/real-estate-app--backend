namespace AuthService.Domain.DTOs
{
    public class LoginRequestDto
    {
        public string PhoneOrEmail { get; set; } = default!;
        public string Password { get; set; } = default!;
    }
}