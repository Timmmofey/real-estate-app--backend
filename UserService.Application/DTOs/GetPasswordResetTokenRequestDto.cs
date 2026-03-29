namespace UserService.Application.DTOs
{
    public record GetPasswordResetTokenRequestDto(
        string Email,
        string VerificationCode
    );
}
