using Classified.Shared.Constants;
using UserService.Application.DTOs;
using UserService.Domain.Models;


namespace UserService.Application.Abstactions
{
    public interface IUserService
    {
        Task<Guid> CreatePersonUserAsync(CreatePersonUserDto dto, string? photoUrl);
        Task<Guid> CreateCompanyUserAsync(CreateCompanyUserDto dto, string? photoUrl);
        Task<(Guid Id, UserRole Role, bool isDeleted)> VerifyUsersCredentials(string password, string emailOrPhone);

        Task PatchPersonProfileAsync(Guid userId, EditPersonUserDto? updatedProfile, string? newMainPhotoUrl);
        Task PatchCompanyProfileAsync(Guid userId, EditCompanyUserDto? updatedProfile, string? newMainPhotoUrl);
        Task<string?> GetUserMainPhotoUrlByUserId(Guid userId);

        Task SoftDeleteAccount(Guid id);
        Task RestoreDeletedAccount(Guid id);
        Task PermanantlyDeleteAccount(Guid id);
        Task<object?> GetUserProfileInfo(Guid userId, UserRole role);
        Task<Guid?> GetUserIdByEmailAsync(string email);
        Task ChangePasswordAsync(Guid userId, string password);
        Task ChangeEmailAsync(Guid userId, string email);
        Task ChangePhoneNumberAsync(Guid userId, string phoneNumber);
        Task<string?> GetUserEmailById(Guid id);
        Task ChangeUserPasswordWithOldPasswordVerification(Guid userId, string oldPassord, string newPassword);
     }
}
