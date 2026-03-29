namespace AuthService.Domain.DTOs
{
    public record LoginRequestDto
    (
        string PhoneOrEmail,
        string Password
    );
}