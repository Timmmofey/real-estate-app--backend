namespace UserService.Application.DTOs
{
    public class GetPasswordResetTokenDto
    {
        public string Email { get; set; } = default!;
        public string VerificationCode { get; set; } = default!;
    }
}
