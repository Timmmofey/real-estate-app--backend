using Classified.Shared.Constants;
using Microsoft.AspNetCore.Http;

namespace UserService.Application.DTOs
{
    public class CreatePersonUserOAuthDto
    {
        public required string Email { get; set; }
        public required string PhoneNumber { get; set; }
        public  required string FirstName { get; set; } 
        public required string LastName { get; set; } 
        public required string ProviderUserId { get; set; } 
        public required OAuthProvider Provider { get; set; } 
        public string? Password { get; set; }
        public string? Country { get; set; }
        public string? Region { get; set; }
        public string? Settlement { get; set; }
        public string? ZipCode { get; set; }
        public IFormFile? MainPhoto { get; set; }
    }
}