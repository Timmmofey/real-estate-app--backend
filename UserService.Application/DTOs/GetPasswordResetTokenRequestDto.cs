using System.ComponentModel.DataAnnotations;

namespace UserService.Application.DTOs
{
    public record GetPasswordResetTokenRequestDto(
        [Required]
        string Email,
        [Required]
        string VerificationCode
    );
}
