namespace UserService.Application.DTOs
{
    public class GetEmailResetTokenViaEmailDto
    {
        public string verificationCode { get; set; } = default!;
    }
}
