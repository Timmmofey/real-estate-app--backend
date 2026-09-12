using System.ComponentModel.DataAnnotations;

namespace AuthService.Domain.DTOs
{
    public record LoginRequestDto
    (
        [Required]
        string PhoneOrEmail,
        [Required]
        string Password
    );
}