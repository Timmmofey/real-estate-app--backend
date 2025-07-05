using AuthService.Domain.DTOs;
using FluentValidation;
using System.Text.RegularExpressions;

namespace AuthService.Application.Validators
{
    public class LoginRequestValidator : AbstractValidator<LoginRequestDto>
    {
        public LoginRequestValidator()
        {
            RuleFor(x => x.PhoneOrEmail)
                .NotEmpty().WithMessage("Phone or Email is required")
                .Must(BeValidPhoneOrEmail).WithMessage("Must be a valid email or phone number");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(5).WithMessage("Password must be at least 5 characters");
        }

        private bool BeValidPhoneOrEmail(string value)
        {
            return IsValidEmail(value) || IsValidPhone(value);
        }

        private bool IsValidEmail(string email)
        {
            // Простая проверка email
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private bool IsValidPhone(string phone)
        {
            // Пример простой проверки телефона — цифры, +, -, пробелы, длина от 7 до 15 символов
            if (string.IsNullOrWhiteSpace(phone))
                return false;

            var phoneRegex = new Regex(@"^\+?[0-9\s\-]{7,15}$");
            return phoneRegex.IsMatch(phone);
        }
    }
}
