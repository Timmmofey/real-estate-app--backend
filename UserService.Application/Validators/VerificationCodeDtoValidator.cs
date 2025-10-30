using Classified.Shared.DTOs;
using FluentValidation;

namespace UserService.Application.Validators
{
    public class VerififcationCodeDtoValidator : AbstractValidator<VerificationCodeDto>
    {
        public VerififcationCodeDtoValidator()
        {
            RuleFor(x => x.Code)
                .Length(10).WithMessage("Verification code must be 10 characters long");
        }
    }
}
