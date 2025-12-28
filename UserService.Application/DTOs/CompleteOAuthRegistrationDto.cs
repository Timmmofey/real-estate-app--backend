using Microsoft.AspNetCore.Http;

namespace UserService.Application.DTOs
{
    public class CompleteOAuthRegistrationDto
    {
        public string UserRole { get; set; } = default!;
        public string PhoneNumber { get; set; } = default!;
        public IFormFile? MainPhoto { get; set; }


        public string? Password { get; set; }

        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Country { get; set; }
        public string? Region { get; set; }
        public string? Settlement { get; set; }
        public string? ZipCode { get; set; }


        public string? Name { get; set; }
        public string? RegistrationAdress { get; set; }
        public string? СompanyRegistrationNumber { get; set; }
        public string? Description { get; set; }
    }

}
