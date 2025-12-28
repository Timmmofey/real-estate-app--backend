using Classified.Shared.Constants;
using Classified.Shared.DTOs;
using UserService.Application.DTOs;
using UserService.Domain.Models;

namespace UserService.Application.Abstactions
{
    public interface IUserService
    {
        Task<Guid> CreatePersonUserAsync(CreatePersonUserDto dto);
        Task<Guid> CreateCompanyUserAsync(CreateCompanyUserDto dto);
        Task<Guid> CreatePersonUserFromOAuthAsync(CreatePersonUserOAuthDto dto);
        Task<Guid> CreateCompanyUserFromOAuthAsync(CreateCompanyUserOAuthDto dto);
        Task<VerifiedUserDto> VerifyUsersCredentials(string emailOrPhone, string password);

        Task PatchPersonProfileAsync(Guid userId, EditPersonUserRequest updatedProfile);
        Task PatchCompanyProfileAsync(Guid userId, EditCompanyUserRequest updatedProfile);
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
        Task<VerifiedUserDto?> GetVerifiedUserDtoById(Guid id);
        Task ChangeUserPasswordWithOldPasswordVerification(Guid userId, string oldPassord, string newPassword);
        Task RequestToggleTwoFactorAuthenticationCode(Guid userId);
        Task ToggleTwoFactorAuthentication(Guid userId, string verificationCode);
        Task StartPasswordResetViaEmail(string email);
        Task<string> GetPasswordResetTokenViaEmail(GetPasswordResetTokenDto dto);
        Task startEmailChangeViaEmailViaEmail(Guid userId);
        Task<string> getResetEmailToken(Guid userId, string verificationCode);
        Task sendCofirmationCodeToNewEmail(Guid userId, string email);

        Task<string> confirmNewEmail(Guid userId, string verificationCode);
     }
}
