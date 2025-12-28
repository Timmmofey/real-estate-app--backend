using UserService.Domain.Models;

namespace UserService.Domain.Abstactions
{
    public interface IUserRepository
    {
        Task<User?> GetUserById(Guid userId);
        Task AddUserAsync(User user);
        Task AddPersonProfileAsync(PersonProfile profile);
        Task AddCompanyProfileAsync(CompanyProfile profile);
        Task AddPersonUserAsync(User user, PersonProfile profile);
        Task AddCompanyUserAsync(User user, CompanyProfile profile);
        Task<User?> FindUserByEmailOrPhoneAsync(string? email, string? phoneNumber);
        Task SoftDeleteUserAsync(Guid userId);
        Task RestoreUserAsync(Guid userId);
        Task PermanantlyDeleteUserAsync(Guid userId);
        Task PatchPersonProfileAsync(Guid userId, string? firstName, string? lastName, string? mainPhotoUrl, string? country, string? region, string? settlement, string? zipCode);
        Task PatchCompanyProfileAsync(Guid userId, string? name, string? country, string? region, string? settlement, string? zipCode, string? registrationAdress, string? companyRegistrationNumber, DateOnly? estimatedAt, string? mainPhotoUrl, string? description);
        Task PatchUserInfoAsync(Guid userId, string? email = null, string? phoneNumber = null, string? passwordHash = null);
        Task<string?> GetUserMainPhotoUrlByUserId(Guid userId);
        Task<PersonProfile?> GetPersonUserInfoByIdAsync(Guid id);
        Task<CompanyProfile?> GetCompanyUserInfoByIdAsync(Guid id);
        Task<Guid?> GetUserIdByEmailAsync(string email);
        Task<string?> GetUserEmailById(Guid id);
        Task<string> GetPasswordHashByUserId(Guid userId);
        Task ToggleTwoFactorAuthentication(Guid userId);
    }
}
