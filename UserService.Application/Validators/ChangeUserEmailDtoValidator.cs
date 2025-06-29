using FluentValidation;
using UserService.Application.DTOs;

namespace UserService.Application.Validators
{
    public class ChangeUserEmailDtoValidator: AbstractValidator<ChangeUserEmailDto>
    {
        public ChangeUserEmailDtoValidator() 
        {
            RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");
        }
        
    }
}
