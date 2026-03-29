using Microsoft.AspNetCore.Http;

namespace UserService.Application.DTOs
{
    public record CreatePersonUserRequestDto : CreateUserBaseAbstract
    {
        public required string FirstName { get; set; }
        public required string LastName { get; set; } 
        public string? Country { get; set; }
        public string? Region { get; set; }
        public string? Settlement { get; set; }
        public string? ZipCode { get; set; }
    }
}