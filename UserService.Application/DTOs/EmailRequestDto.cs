using System.ComponentModel.DataAnnotations;

namespace UserService.Application.DTOs
{
    public record EmailRequestDto
    (
        [Required]
        string Email
    );
}
