namespace UserService.Application.DTOs
{
    public record VerifyUserCredentialsRequestDto(string PhoneOrEmail, string Password);
}
