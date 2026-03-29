using Microsoft.AspNetCore.Http;

namespace UserService.Application.DTOs
{
    public abstract record CreateUserBaseAbstract
    {
        public required string Email { get; set; } 
        public required string Password { get; set; } 
        public required string PhoneNumber { get; set; } 
        public IFormFile? MainPhoto { get; set; }
    }
}