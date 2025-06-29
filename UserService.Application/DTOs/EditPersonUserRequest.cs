using Microsoft.AspNetCore.Http;

namespace UserService.Application.DTOs
{
    public class EditPersonUserRequest
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Country { get; set; }
        public string? Region { get; set; }
        public string? Settlement { get; set; }
        public string? ZipCode { get; set; }
        public IFormFile? MainPhoto { get; set; }
        public bool? DeleteMainPhoto { get; set; }
    }
}
