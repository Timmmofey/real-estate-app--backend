namespace UserService.Application.DTOs
{
    public record ChangeUserPassordRequestDto
    (
         string OldPassword,
         string NewPassword
    );
}
