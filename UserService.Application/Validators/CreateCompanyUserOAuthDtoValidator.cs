using FluentValidation;
using UserService.Application.DTOs;

namespace UserService.Application.Validators
{
    public class CreateCompanyUserOAuthDtoValidator : AbstractValidator<CreateCompanyUserOAuthDto>
    {
        public CreateCompanyUserOAuthDtoValidator()
        {

            RuleFor(x => x.Country).NotEmpty();
            RuleFor(x => x.Region).NotEmpty();
            RuleFor(x => x.Settlement).NotEmpty();
            RuleFor(x => x.ZipCode).NotEmpty();

        }
    }
}
