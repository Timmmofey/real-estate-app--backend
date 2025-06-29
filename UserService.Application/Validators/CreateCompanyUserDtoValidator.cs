using FluentValidation;
using UserService.Application.DTOs;

public class CreateCompanyUserDtoValidator : AbstractValidator<CreateCompanyUserDto>
{
    public CreateCompanyUserDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password hash is required.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Company name is required.");
    }
}
