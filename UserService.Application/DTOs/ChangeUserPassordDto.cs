namespace UserService.Application.DTOs
{
    public class ChangeUserPassordDto
    {
        public string OldPassword { get; set; } = default!;
        public string NewPassword { get; set; } = default!;

    }
}
