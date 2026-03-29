namespace UserService.Application.DTOs
{
    public record EditPersonUserRequestDto(
        string? FirstName,
        string? LastName,
        string? Country,
        string? Region,
        string? Settlement,
        string? ZipCode
    );
}
