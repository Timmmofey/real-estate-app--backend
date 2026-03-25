using UserService.Domain.Models;

namespace UserService.Domain.Abstactions
{
    public interface IUserRepository
    {
        Task<User?> GetUserById(Guid userId, CancellationToken ct);
        Task AddUserAsync(User user, CancellationToken ct);
        Task AddPersonProfileAsync(PersonProfile profile, CancellationToken ct);
        Task AddCompanyProfileAsync(CompanyProfile profile, CancellationToken ct);
        Task AddPersonUserAsync(User user, PersonProfile profile, CancellationToken ct);
        Task AddCompanyUserAsync(User user, CompanyProfile profile, CancellationToken ct);
        Task<User?> FindUserByEmailOrPhoneAsync(CancellationToken ct, string? email, string? phoneNumber);
        Task SoftDeleteUserAsync(Guid userId, CancellationToken ct);
        Task RestoreUserAsync(Guid userId, CancellationToken ct);
        Task PermanantlyDeleteUserAsync(Guid userId, CancellationToken ct);
        Task PatchPersonProfileAsync(Guid userId, CancellationToken ct, string? firstName, string? lastName, string? mainPhotoUrl, string? country, string? region, string? settlement, string? zipCode);
        Task PatchCompanyProfileAsync(Guid userId, CancellationToken ct, string? name, string? country, string? region, string? settlement, string? zipCode, string? registrationAdress, string? companyRegistrationNumber, DateOnly? estimatedAt, string? mainPhotoUrl, string? description);
        Task PatchUserInfoAsync(Guid userId, CancellationToken ct, string? email = null, string? phoneNumber = null, string? passwordHash = null);
        Task<string?> GetUserMainPhotoUrlByUserId(Guid userId, CancellationToken ct);
        Task<PersonProfile?> GetPersonUserInfoByIdAsync(Guid id, CancellationToken ct);
        Task<CompanyProfile?> GetCompanyUserInfoByIdAsync(Guid id, CancellationToken ct);
        Task<Guid?> GetUserIdByEmailAsync(string email, CancellationToken ct);
        Task<string?> GetUserEmailById(Guid id, CancellationToken ct);
        Task<string?> GetPasswordHashByUserId(Guid userId, CancellationToken ct);
        Task ToggleTwoFactorAuthentication(Guid userId, CancellationToken ct);
        Task<User?> GetUserByEmail(string email, CancellationToken ct);
        Task<User?> GetUserByPhoneNumber(string phoneNumber, CancellationToken ct);
    }
}
