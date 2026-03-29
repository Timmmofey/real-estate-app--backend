using Classified.Shared.Constants;
using Microsoft.AspNetCore.Http;

namespace UserService.Application.DTOs
{
    public record CreateCompanyUserOAuthRequestDto
    {
        public required string Email { get; set; }
        public required string PhoneNumber { get; set; } 
        public required string ProviderUserId { get; set; } 
        public required OAuthProvider Provider { get; set; } 
        public required string Name { get; set; } 
        public required string Country { get; set; } 
        public required string Region { get; set; } 
        public required string Settlement { get; set; } 
        public required string ZipCode { get; set; } 
        public required string RegistrationAdress { get; set; } 
        public required string СompanyRegistrationNumber { get; set; } 
        public string? Password { get; set; } 
        public DateOnly EstimatedAt { get; set; }
        public string? Description { get; set; }
        public IFormFile? MainPhoto { get; set; }
    }
}
