using Classified.Shared.Constants;
using UserService.Application.DTOs;
using UserService.Domain.Models;

namespace UserService.Application.Abstactions
{
    public interface IUserService
    {
        Task<Guid> CreatePersonUserAsync(CreatePersonUserDto dto, string? photoUrl);
        Task<Guid> CreateCompanyUserAsync(CreateCompanyUserDto dto, string? photoUrl);
        Task<(Guid Id, string email, UserRole Role, bool isDeleted, bool isTwoFactorEnabled)> VerifyUsersCredentials(string emailOrPhone, string password);

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
        Task<User?> GetUserById(Guid id);
        Task ChangeUserPasswordWithOldPasswordVerification(Guid userId, string oldPassord, string newPassword);
        Task SetTwoFactorAuthentication(string userId, bool flag);
     }
}
