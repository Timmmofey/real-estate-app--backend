using System.ComponentModel.DataAnnotations;

namespace UserService.Application.DTOs
{
    public record UserIdRequestDto([Required] string UserId);

}
