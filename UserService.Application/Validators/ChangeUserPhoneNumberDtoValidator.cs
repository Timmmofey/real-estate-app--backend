using FluentValidation;
using UserService.Application.DTOs;

namespace UserService.Application.Validators
{
    public class ChangeUserPhoneNumberDtoValidator: AbstractValidator<ChangeUserPhoneNumberDto>
    {
        public ChangeUserPhoneNumberDtoValidator() 
        {
            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Phone nunber is required")
                .Length(11).WithMessage("Enter valid mobile phone number");
        }
    }
}
