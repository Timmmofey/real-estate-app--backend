using Microsoft.AspNetCore.Http;

namespace UserService.Application.DTOs
{
    public record EditCompanyUserRequestDto(
        string? Name,
        string? Country,
        string? Region,
        string? Settlement,
        string? ZipCode,
        string? RegistrationAdress,
        string? СompanyRegistrationNumber,
        DateOnly? EstimatedAt,
        string? Description,
        IFormFile? MainPhoto,
        bool? DeleteMainPhoto
    );
}
