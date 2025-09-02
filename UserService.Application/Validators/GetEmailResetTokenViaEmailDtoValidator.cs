using FluentValidation;
using UserService.Application.DTOs;

namespace UserService.Application.Validators
{
    public class EmailResetVerificationCodeDtoValidator : AbstractValidator<EmailResetVerificationCodeDto>
    {
        public EmailResetVerificationCodeDtoValidator()
        {
            RuleFor(x => x.verificationCode)
                .Length(10).WithMessage("Verification code must be 10 characters long");
        }
    }
}
