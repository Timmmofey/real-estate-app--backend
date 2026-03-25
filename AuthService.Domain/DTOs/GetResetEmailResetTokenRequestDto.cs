namespace UserService.Application.DTOs
{
    public record GetResetEmailResetTokenRequestDto(string userId, string newEmail);

}
