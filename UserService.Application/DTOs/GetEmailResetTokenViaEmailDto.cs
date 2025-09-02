namespace UserService.Application.DTOs
{
    public class GetEmailResetTokenViaEmailDto
    {
        public string verificationCode { get; set; } = default!;
    }

    public class EmailResetVerificationCodeDto
    {
        public string verificationCode { get; set; } = default!;
    }

    public class EmailDto
    {
        public string email { get; set; } = default!;
    }
}
