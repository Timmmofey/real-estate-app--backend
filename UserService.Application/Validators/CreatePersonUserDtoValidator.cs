using FluentValidation;
using UserService.Application.DTOs;

public class CreatePersonUserDtoValidator : AbstractValidator<CreatePersonUserDto>
{
    public CreatePersonUserDtoValidator(CreateUserBaseValidator baseValidator)
    {
        Include(baseValidator);

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(70);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(70);
    }
}
