using FluentValidation;
using Microsoft.Extensions.Localization;
using UserService.Application.DTOs;

public class CreateUserBaseValidator
    : AbstractValidator<CreateUserBaseAbstract>
{
    public CreateUserBaseValidator(IStringLocalizer<CreateUserBaseValidator> localizer)
    {
        RuleFor(x => x.Email)
           .NotEmpty().WithMessage("Email is required.")
           .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.PhoneNumber)
           .NotEmpty().WithMessage("Phone number is required.")
           .Length(11).WithMessage("Phone number must be 11 digits.")
           .Matches(@"^\d+$").WithMessage("Phone number must contain only digits.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(5).WithMessage("Minimal password length is 5 characters");
    }
}

