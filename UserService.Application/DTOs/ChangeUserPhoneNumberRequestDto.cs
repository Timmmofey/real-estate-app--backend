using System.ComponentModel.DataAnnotations;

namespace UserService.Application.DTOs
{
    public record ChangeUserPhoneNumberRequestDto([Required] string PhoneNumber);
}
