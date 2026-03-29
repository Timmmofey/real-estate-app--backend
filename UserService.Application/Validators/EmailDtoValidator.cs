using FluentValidation;
using UserService.Application.DTOs;

namespace UserService.Application.Validators
{
    public class EmailDtoValidator : AbstractValidator<EmailRequestDto>
    {
        public EmailDtoValidator()
        {
            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("Enter apropriate email adress");
        }
    }
}
