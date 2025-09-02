using FluentValidation;
using UserService.Application.DTOs;

namespace UserService.Application.Validators
{
    public class EmailDtoValidator : AbstractValidator<EmailDto>
    {
        public EmailDtoValidator()
        {
            RuleFor(x => x.email)
                .EmailAddress().WithMessage("Enter apropriate email adress");
        }
    }
}
