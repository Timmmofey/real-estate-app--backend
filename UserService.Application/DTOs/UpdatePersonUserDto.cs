using Microsoft.AspNetCore.Http;

namespace UserService.Application.DTOs
{
    public class UpdatePersonUserDto
    {
        public string? FirstName { get; set; } = default!;
        public string? LastName { get; set; } = default!;
        public string? Location { get; set; }
        public string? Description { get; set; }
        public IFormFile? MainPhoto { get; set; }
    }

}
