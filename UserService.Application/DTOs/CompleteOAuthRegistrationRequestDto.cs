using Microsoft.AspNetCore.Http;

namespace UserService.Application.DTOs
{
    public record CompleteOAuthRegistrationRequestDto(
        string UserRole,
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
