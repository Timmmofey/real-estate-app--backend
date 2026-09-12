using System.ComponentModel.DataAnnotations;

namespace UserService.Application.DTOs
{
    public record ChangeUserPassordRequestDto
    (
        [Required]
         string OldPassword,
        [Required]
         string NewPassword
    );
}
