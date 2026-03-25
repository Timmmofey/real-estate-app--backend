using Classified.Shared.Constants;
using Classified.Shared.DTOs;
using UserService.Application.DTOs;
using UserService.Domain.Models;

namespace UserService.Application.Abstactions
{
    public interface IUserService
    {
        Task<Guid> CreatePersonUserAsync(CreatePersonUserDto dto, CancellationToken ct);
        Task<Guid> CreateCompanyUserAsync(CreateCompanyUserDto dto, CancellationToken ct);
        Task<Guid> CreatePersonUserFromOAuthAsync(CreatePersonUserOAuthDto dto, CancellationToken ct);
        Task<Guid> CreateCompanyUserFromOAuthAsync(CreateCompanyUserOAuthDto dto, CancellationToken ct);
        Task<VerifiedUserDto> VerifyUsersCredentials(string emailOrPhone, string password, CancellationToken ct);

        Task PatchPersonProfileAsync(Guid userId, EditPersonUserRequest updatedProfile, CancellationToken ct);
        Task PatchCompanyProfileAsync(Guid userId, EditCompanyUserRequest updatedProfile, CancellationToken ct);
        Task<string?> GetUserMainPhotoUrlByUserId(Guid userId, CancellationToken ct);

        Task SoftDeleteAccount(Guid id, CancellationToken ct);
        Task RestoreDeletedAccount(Guid id, CancellationToken ct);
        Task PermanantlyDeleteAccount(Guid id, CancellationToken ct);
        Task<object?> GetUserProfileInfo(Guid userId, UserRole role, CancellationToken ct);
        Task<Guid?> GetUserIdByEmailAsync(string email, CancellationToken ct);
        Task ChangePasswordAsync(Guid userId, string password, CancellationToken ct);
        Task ChangeEmailAsync(Guid userId, string email, CancellationToken ct);
        Task ChangePhoneNumberAsync(Guid userId, string phoneNumber, CancellationToken ct);
        Task<User?> GetUserById(Guid id, CancellationToken ct);
        Task<VerifiedUserDto?> GetVerifiedUserDtoById(Guid id, CancellationToken ct);
        Task ChangeUserPasswordWithOldPasswordVerification(Guid userId, string oldPassord, string newPassword, CancellationToken ct);
        Task RequestToggleTwoFactorAuthenticationCode(Guid userId, CancellationToken ct);
        Task ToggleTwoFactorAuthentication(Guid userId, string verificationCode, CancellationToken ct);
        Task StartPasswordResetViaEmail(string email, CancellationToken ct);
        Task<string> GetPasswordResetTokenViaEmail(GetPasswordResetTokenDto dto, CancellationToken ct);
        Task StartEmailChangeViaEmailViaEmail(Guid userId, CancellationToken ct);
        Task<string> GetResetEmailToken(Guid userId, string verificationCode, CancellationToken ct);
        Task SendCofirmationCodeToNewEmail(Guid userId, string email, CancellationToken ct);

        Task<string> ConfirmNewEmail(Guid userId, string verificationCode, CancellationToken ct);
     }
}
