using Microsoft.AspNetCore.Http;

namespace UserService.Application.DTOs
{
    public class CreateCompanyUserDto{
        public string Email { get; set; } = default!;
        public string Password { get; set; } = default!;
        public string PhoneNumber { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string Country { get; set; } = default!;
        public string Region { get; set; } = default!;
        public string Settlement { get; set; } = default!;
        public string ZipCode { get; set; } = default!;
        public string RegistrationAdress { get; set; } = default!;
        public string СompanyRegistrationNumber { get; set; } = default!;
        public DateOnly EstimatedAt { get; }
        public string? Description { get; set; }
        public IFormFile? MainPhoto { get; set; }
    }
}
