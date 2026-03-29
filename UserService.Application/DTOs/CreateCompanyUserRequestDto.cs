using Microsoft.AspNetCore.Http;

namespace UserService.Application.DTOs
{
    public record CreateCompanyUserRequestDto : CreateUserBaseAbstract
    {
        public required string Name { get; set; } 
        public required string Country { get; set; } 
        public required string Region { get; set; } 
        public required string Settlement { get; set; } 
        public required string ZipCode { get; set; } 
        public required string RegistrationAdress { get; set; } 
        public required string СompanyRegistrationNumber { get; set; } 
        public DateOnly EstimatedAt { get; set; }
        public string? Description { get; set; }
    }
}