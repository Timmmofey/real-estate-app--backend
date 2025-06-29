using FluentValidation;
using UserService.Application.DTOs;

namespace UserService.Application.Validators
{
    public class GetPasswordResetTokenDtoValidator: AbstractValidator<GetPasswordResetTokenDto>
    {
        public GetPasswordResetTokenDtoValidator() {
            RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

            RuleFor(x => x.VerificationCode)
            .NotEmpty().WithMessage("Verification code is required.")
            .Length(10).WithMessage("Verification code must be 10 characters long.");
        }
    }
}
