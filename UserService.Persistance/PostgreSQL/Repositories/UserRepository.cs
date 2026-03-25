using Classified.Shared.Constants;
using Classified.Shared.Entities;
using Microsoft.EntityFrameworkCore;
using UserService.Domain.Abstactions;
using UserService.Domain.Models;
using UserService.Persistance.PostgreSQL.Entities;

namespace UserService.Persistance.PostgreSQL.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly UserServicePostgreDbContext _context;

        public UserRepository(UserServicePostgreDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetUserById(Guid userId, CancellationToken ct)
        {
            var userEntity = await _context.Users.FirstOrDefaultAsync(user => user.Id == userId, ct);

            if (userEntity == null) return null;
            
            return MapUserEntityToExistingDomain(userEntity);
        }

        public async Task AddUserAsync(User user, CancellationToken ct)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            var userEntity = MapToUserEntity(user);

            await _context.Users.AddAsync(userEntity, ct);
        }

        public async Task AddPersonProfileAsync(PersonProfile profile, CancellationToken ct)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));

            var profileEntity = MapToEntity(profile);

            await _context.Set<PersonProfileEntity>().AddAsync(profileEntity, ct);
        }

        public async Task AddCompanyProfileAsync(CompanyProfile profile, CancellationToken ct)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));

            var profileEntity = MapToEntity(profile);

            await _context.Set<CompanyProfileEntity>().AddAsync(profileEntity, ct);
        }

        public async Task AddPersonUserAsync(User user, PersonProfile profile, CancellationToken ct)
        {
            var userEntity = MapToUserEntity(user);

            var profileEntity = MapToEntity(profile);

            await AddUserWithProfileAsync(userEntity, profileEntity, ct);
        }

        public async Task AddCompanyUserAsync(User user, CompanyProfile profile, CancellationToken ct)
        {
            var userEntity = MapToUserEntity(user);

            var profileEntity = MapToEntity(profile);

            await AddUserWithProfileAsync(userEntity, profileEntity, ct);
        }

        public async Task<User?> FindUserByEmailOrPhoneAsync(CancellationToken ct, string? email, string? phoneNumber)
        {
            var userEntity = await _context.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => (u.Email == email || u.PhoneNumber == phoneNumber) && u.IsPermanantlyDeleted != true, ct);

            if (userEntity == null)
            {
                return null;
            }

            return MapUserEntityToExistingDomain(userEntity);
        }

        public async Task SoftDeleteUserAsync(Guid userId, CancellationToken ct)
        {
            var deletedUser = await _context.Users
                .Where(u => u.Id == userId)
                .ExecuteUpdateAsync(setter => setter
                    .SetProperty(u => u.IsSoftDeleted, true)
                    .SetProperty(u => u.DeletedAt, DateTime.UtcNow), ct);

            if (deletedUser == 0) throw new InvalidOperationException("User not found");
        }

        public async Task RestoreUserAsync(Guid userId, CancellationToken ct)
        {
            var deletedUser = await _context.Users
                .Where(u => u.Id == userId)
                .ExecuteUpdateAsync(setter => setter
                    .SetProperty(u => u.IsSoftDeleted, false)
                    .SetProperty(u => u.DeletedAt, (DateTime?)null), ct);

            if (deletedUser == 0) throw new InvalidOperationException("User not found");
        }

        public async Task PermanantlyDeleteUserAsync(Guid userId, CancellationToken ct)
        {
            var deletedUser = await _context.Users
                .Where(u => u.Id == userId)
                .ExecuteUpdateAsync(setter => setter
                    .SetProperty(u => u.IsPermanantlyDeleted, true), ct);

            if (deletedUser == 0) throw new InvalidOperationException("User not found");
        }

        public async Task PatchPersonProfileAsync
        (
            Guid userId,
            CancellationToken ct,
            string? firstName,
            string? lastName,
            string? mainPhotoUrl,
            string? country,
            string? region,
            string? settlement,
            string? zipCode
        )
        {
            var profile = await _context.PersonProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
            if (profile == null) throw new InvalidOperationException("Person profile not found");

            if (!string.IsNullOrEmpty(firstName))
                profile.FirstName = firstName;

            if (!string.IsNullOrEmpty(lastName))
                profile.LastName = lastName;

            if (!string.IsNullOrEmpty(mainPhotoUrl))
                profile.MainPhotoUrl = mainPhotoUrl == "__DELETE__" ? null : mainPhotoUrl;

            if (!string.IsNullOrEmpty(country))
                profile.Country = country == "__DELETE__" ? null : country;

            if (!string.IsNullOrEmpty(region))
                profile.Region = region == "__DELETE__" ? null : region;

            if (!string.IsNullOrEmpty(settlement))
                profile.Settlement = settlement == "__DELETE__" ? null : settlement;

            if (!string.IsNullOrEmpty(zipCode))
                profile.ZipCode = zipCode == "__DELETE__" ? null : zipCode;

            if (country == "__DELETE__")
            {
                profile.Country = null;
                profile.Region = null;
                profile.Settlement = null;
                profile.ZipCode = null;
            }

            if (region == "__DELETE__")
            {
                profile.Region = null;
                profile.Settlement = null;
                profile.ZipCode = null;
            }

            if (settlement == "__DELETE__")
            {
                profile.Settlement = null;
                profile.ZipCode = null;
            }

            _context.PersonProfiles.Update(profile);
            await _context.SaveChangesAsync(ct);
        }

        public async Task PatchCompanyProfileAsync
        (
            Guid userId,
            CancellationToken ct,
            string? name, 
            string? country, 
            string? region, 
            string? settlement, 
            string? zipCode, 
            string? registrationAdress, 
            string? companyRegistrationNumber, 
            DateOnly? estimatedAt, 
            string? mainPhotoUrl, 
            string? description
        )
        {
            var user = await _context.Users.FirstOrDefaultAsync(p => p.Id == userId);
            if ( user == null ) throw new InvalidOperationException("User not found");
            var profile = await _context.CompanyProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
            if (profile == null) throw new InvalidOperationException("Company profile not found");

            user.IsVerified = false;

            if (!string.IsNullOrEmpty(name))
                profile.Name = name;

            if (!string.IsNullOrEmpty(country))
                profile.Country = country;

            if (!string.IsNullOrEmpty(region))
                profile.Region = region;

            if (!string.IsNullOrEmpty(settlement))
                profile.Settlement = settlement;

            if (!string.IsNullOrEmpty(zipCode))
                profile.ZipCode = zipCode;

            if (!string.IsNullOrEmpty(registrationAdress))
                profile.RegistrationAdress = registrationAdress;

            if (!string.IsNullOrEmpty(companyRegistrationNumber))
                profile.СompanyRegistrationNumber = companyRegistrationNumber;

            if (estimatedAt != null)
                profile.EstimatedAt = (DateOnly)estimatedAt;

            if (!string.IsNullOrEmpty(mainPhotoUrl) && mainPhotoUrl != "__DELETE__")
                profile.MainPhotoUrl = mainPhotoUrl == "__DELETE__" ? null : mainPhotoUrl;

            if (!string.IsNullOrEmpty(description))
                profile.Description = description == "__DELETE__" ? null : description;

            await _context.SaveChangesAsync(ct);
        }

        public async Task PatchUserInfoAsync(Guid userId, CancellationToken ct, string? email = null, string? phoneNumber = null, string? passwordHash = null)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);

            if (user == null) throw new InvalidOperationException("User has not been found");

            if (!string.IsNullOrEmpty(email))
            {
                var usersWithThisEmail = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsPermanantlyDeleted == false);

                if (usersWithThisEmail != null) throw new InvalidOperationException("User with this email already exists");

                user.Email = email;
            }

            if (!string.IsNullOrEmpty(phoneNumber))
            {
                var usersWithThisPhoneNumber= await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber && u.IsPermanantlyDeleted == false);

                if (usersWithThisPhoneNumber != null) throw new InvalidOperationException("User with this phone number already exists");
            }

            if (!string.IsNullOrEmpty(passwordHash))
                user.PasswordHash = passwordHash;

            await _context.SaveChangesAsync(ct);
        }

        public async Task<string?> GetUserMainPhotoUrlByUserId(Guid userId, CancellationToken ct)
        {
            var user = await _context.Users
                .Include(u => u.PersonProfile)
                .Include(u => u.CompanyProfile)
                .FirstOrDefaultAsync(u => u.Id == userId, ct);

            if (user == null) return null;

            return user.PersonProfile?.MainPhotoUrl ?? user.CompanyProfile?.MainPhotoUrl;
        }

        public async Task<PersonProfile?> GetPersonUserInfoByIdAsync(Guid id, CancellationToken ct)
        {
            var personUserEntity = await _context.PersonProfiles
                .FirstOrDefaultAsync(p => p.UserId == id, ct);

            if (personUserEntity == null)
            {
                return null;
            }

            var (person, error) = PersonProfile.Create(id, personUserEntity!.FirstName, personUserEntity.LastName, personUserEntity?.MainPhotoUrl, personUserEntity?.Country, personUserEntity?.Region, personUserEntity?.Settlement, personUserEntity?.ZipCode);

            if (person == null)
            {
                throw new Exception($"User entity is invalid: {error}");
            }

            return person;
        }

        public async Task<CompanyProfile?> GetCompanyUserInfoByIdAsync(Guid id, CancellationToken ct)
        {
            var companyUserEnity= await _context.CompanyProfiles.FirstOrDefaultAsync(p => p.UserId == id, ct);

            if (companyUserEnity == null)
            {
                return null;
            }

            var (company, error) = CompanyProfile.Create(id, companyUserEnity!.Name, companyUserEnity.Country, companyUserEnity.Region, companyUserEnity.Settlement, companyUserEnity.ZipCode, companyUserEnity.RegistrationAdress, companyUserEnity.СompanyRegistrationNumber, companyUserEnity.EstimatedAt, companyUserEnity?.MainPhotoUrl, companyUserEnity?.Description);

            if (company == null)
            {
                throw new Exception($"User entity is invalid: {error}");
            }

            return company;
        }

        public async Task<Guid?> GetUserIdByEmailAsync(string email, CancellationToken ct)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email.Trim() && u.IsPermanantlyDeleted == false, ct);

            return user?.Id;
        }

        public async Task<string?> GetUserEmailById(Guid id, CancellationToken ct)
        {
            var userEntity = await _context.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

            if (userEntity == null)
                throw new Exception("User with such id doesnt exist");

           var email = userEntity.Email;

            return email;
        }

        public async Task<string?> GetPasswordHashByUserId(Guid userId, CancellationToken ct)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user == null) throw new InvalidOperationException("User not found");

            var passwordHash = user.PasswordHash;

            return passwordHash;
        }

        public async Task ToggleTwoFactorAuthentication(Guid userId, CancellationToken ct)
        {
            var user = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => new { u.IsTwoFactorEnabled })
                .SingleOrDefaultAsync(ct);

            if (user == null)
                throw new InvalidOperationException("User not found");

            var newValue = !user.IsTwoFactorEnabled;

            var affected = await _context.Users
                .Where(u => u.Id == userId)
                .ExecuteUpdateAsync(setter => setter
                    .SetProperty(u => u.IsTwoFactorEnabled, newValue), ct);

            if (affected == 0)
                throw new InvalidOperationException("User not found");
        }

        public async Task<User?> GetUserByEmail(string email, CancellationToken ct)
        {
            var userEntity = await _context.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => (u.Email == email && u.IsPermanantlyDeleted != true), ct);

            if (userEntity == null)
                return null;

            return MapUserEntityToExistingDomain(userEntity);
        }

        public async Task<User?> GetUserByPhoneNumber(string phoneNumber, CancellationToken ct)
        {
            var userEntity = await _context.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => (u.PhoneNumber == phoneNumber && u.IsPermanantlyDeleted != true), ct);

            if (userEntity == null)
            {
                return null;
            }

            return MapUserEntityToExistingDomain(userEntity);
        }



        /// <summary>
        /// Private methods
        /// </summary>

        private UserEntity MapToUserEntity (User user)
        {
            var userEntity = new UserEntity
            {
                Id = user.Id,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                PasswordHash = user.PasswordHash,
                Role = (UserRoleEntity)user.Role,
                IsVerified = user.IsVerified,
                IsBlocked = user.IsBlocked,
                IsSoftDeleted = user.IsSoftDeleted,
                IsPermanantlyDeleted = user.IsPermanantlyDeleted,
                CreatedAt = user.CreatedAt,
            };

            return userEntity;
        }

        private async Task AddUserWithProfileAsync(UserEntity user, object profile, CancellationToken ct)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(ct);
            try
            {
                await _context.Users.AddAsync(user);
                await _context.AddAsync(profile);
                await _context.SaveChangesAsync(ct);

                await transaction.CommitAsync(ct);
            }
            catch { 
                await transaction.RollbackAsync(ct);
                throw;
            }
            finally
            {
                await transaction.DisposeAsync();
            }
        }

        private PersonProfileEntity MapToEntity(PersonProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));

            // Применяем каскадную нормализацию адреса
            var country = NormalizeAddressField(profile.Country);
            var region = NormalizeAddressField(profile.Region);
            var settlement = NormalizeAddressField(profile.Settlement);
            var zipCode = NormalizeAddressField(profile.ZipCode);

            ApplyAddressCascade(ref country, ref region, ref settlement, ref zipCode);

            return new PersonProfileEntity
            {
                UserId = profile.UserId,
                FirstName = profile.FirstName,
                LastName = profile.LastName,
                MainPhotoUrl = profile.MainPhotoUrl,
                Country = country,
                Region = region,
                Settlement = settlement,
                ZipCode = zipCode,
            };
        }

        private CompanyProfileEntity MapToEntity(CompanyProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));

            // Применяем каскадную нормализацию адреса
            var country = NormalizeAddressField(profile.Country);
            var region = NormalizeAddressField(profile.Region);
            var settlement = NormalizeAddressField(profile.Settlement);
            var zipCode = NormalizeAddressField(profile.ZipCode);

            ApplyAddressCascade(ref country, ref region, ref settlement, ref zipCode);

            return new CompanyProfileEntity
            {
                UserId = profile.UserId,
                Name = profile.Name,
                Country = country!,
                Region = region!,
                Settlement = settlement!,
                ZipCode = zipCode!,
                RegistrationAdress = profile.RegistrationAdress,
                СompanyRegistrationNumber = profile.СompanyRegistrationNumber,
                EstimatedAt = profile.EstimatedAt,
                MainPhotoUrl = profile.MainPhotoUrl,
                Description = profile.Description
            };
        }

        private static string? NormalizeAddressField(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var trimmed = value.Trim();
            if (trimmed.Equals("__DELETE__", StringComparison.OrdinalIgnoreCase)) return null;
            if (trimmed.Equals("none", StringComparison.OrdinalIgnoreCase)) return null;
            return trimmed;
        }

        private static void ApplyAddressCascade(
             ref string? country,
             ref string? region,
             ref string? settlement,
             ref string? zipCode)
        {
            country = NormalizeAddressField(country);
            if (country == null)
            {
                region = null;
                settlement = null;
                zipCode = null;
                return;
            }

            region = NormalizeAddressField(region);
            if (region == null)
            {
                settlement = null;
                zipCode = null;
                return;
            }

            settlement = NormalizeAddressField(settlement);
            if (settlement == null)
            {
                zipCode = null;
            }

            zipCode = NormalizeAddressField(zipCode);
        }

        private User? MapUserEntityToExistingDomain(UserEntity userEntity)
        {
            var (user, error) = User.CreateExisting(
                userEntity.Id,
                userEntity.Email,
                userEntity.PhoneNumber,
                (UserRole)userEntity.Role,
                userEntity.PasswordHash,
                userEntity.IsTwoFactorEnabled,
                userEntity.IsVerified,
                userEntity.IsBlocked,
                userEntity.IsSoftDeleted,
                userEntity.IsPermanantlyDeleted,
                userEntity.CreatedAt,
                userEntity.DeletedAt
            );

            if (error != null) { 
                throw new InvalidOperationException(error);
            }

            return user;
        }
    }
}
