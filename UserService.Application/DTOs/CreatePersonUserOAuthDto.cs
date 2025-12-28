using Classified.Shared.Constants;
using Microsoft.AspNetCore.Http;

namespace UserService.Application.DTOs
{
    public class CreatePersonUserOAuthDto
    {
        public string Email { get; set; } = default!;
        public string PhoneNumber { get; set; } = default!;
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public string ProviderUserId { get; set; } = default!;
        public OAuthProvider Provider { get; set; } = default!;
        public string? Password { get; set; }
        public string? Country { get; set; }
        public string? Region { get; set; }
        public string? Settlement { get; set; }
        public string? ZipCode { get; set; }
        public IFormFile? MainPhoto { get; set; }
    }
}