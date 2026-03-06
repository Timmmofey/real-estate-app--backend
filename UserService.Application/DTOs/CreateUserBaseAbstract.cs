using Microsoft.AspNetCore.Http;

namespace UserService.Application.DTOs
{
    public abstract class CreateUserBaseAbstract
    {
        public string Email { get; set; } = default!;
        public string Password { get; set; } = default!;
        public string PhoneNumber { get; set; } = default!;
        public IFormFile? MainPhoto { get; set; }
    }
}