using System.ComponentModel.DataAnnotations;

namespace UserService.Application.DTOs
{
    public record GetResetEmailResetTokenRequestDto([Required] string userId, [Required] string newEmail);

}
