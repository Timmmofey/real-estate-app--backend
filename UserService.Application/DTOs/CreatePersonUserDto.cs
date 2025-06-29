using Microsoft.AspNetCore.Http;

namespace UserService.Application.DTOs
{
    public class CreatePersonUserDto
    {
        public string Email { get; set; } = default!;
        public string Password { get; set; } = default!;
        public string PhoneNumber { get; set; } = default!;
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public string? Country { get; set; }
        public string? Region { get; set; }
        public string? Settlement { get; set; }
        public string? ZipCode { get; set; }
        public IFormFile? MainPhoto { get; set; }
    }
}
