using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace UserService.Application.DTOs
{
    public record CompleteOAuthRegistrationRequestDto(
        [Required]
        string UserRole,
        [Required]
        string PhoneNumber,
        IFormFile? MainPhoto,
        string? Password,
        string? FirstName,
        string? LastName,
        string? Country,
        string? Region,
        string? Settlement,
        string? ZipCode,
        string? Name,
        string? RegistrationAdress,
        string? СompanyRegistrationNumber,
        string? Description
    );

}
