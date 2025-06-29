using FluentValidation;
using UserService.Application.DTOs;

namespace UserService.Application.Validators
{
    public class GetEmailResetTokenViaEmailDtoValidator: AbstractValidator<GetEmailResetTokenViaEmailDto>
    {
        public GetEmailResetTokenViaEmailDtoValidator()
        {
            RuleFor(x => x.verificationCode)
                .Length(10).WithMessage("Verification code must be 10 characters long");
        }
    }
}
