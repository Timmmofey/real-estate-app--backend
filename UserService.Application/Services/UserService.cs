using UserService.Domain.Models;
using UserService.Domain.Abstactions;
using UserService.Application.DTOs;
using UserService.Domain.Exeptions;
using Classified.Shared.Constants;
using UserService.Application.Abstactions;
using Amazon;

namespace UserService.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;


        public UserService(IUserRepository userRepository, IPasswordHasher passwordHasher)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
        }

        public async Task<Guid> CreatePersonUserAsync(CreatePersonUserDto dto, string? photoUrl)
        {
            await FindExistingOrResentlyDeletedUser(dto.Email, dto.PhoneNumber);


            var userId = Guid.NewGuid();
            var hashedPassword = _passwordHasher.Generate(dto.Password);

            var (user, userError) = User.Create(
                userId,
                dto.Email,
                hashedPassword,
                dto.PhoneNumber,
                UserRole.Person
            );

            if (user == null)
                throw new Exception(userError);

            var (profile, profileError) = PersonProfile.Create(
                userId,
                dto.FirstName,
                dto.LastName,
                photoUrl ?? null,
                dto.Country,
                dto.Region,
                dto.Settlement,
                dto.ZipCode
            );

            if (profile == null)
                throw new Exception(profileError);

            await _userRepository.AddPersonUserAsync(user, profile);

            return userId;
        }

        public async Task<Guid> CreateCompanyUserAsync(CreateCompanyUserDto dto, string? photoUrl)
        {
            await FindExistingOrResentlyDeletedUser(dto.Email, dto.PhoneNumber);

            var userId = Guid.NewGuid();
            var hashedPassword = _passwordHasher.Generate(dto.Password);

            var (user, userError) = User.Create(
                userId,
                dto.Email,
                hashedPassword,
                dto.PhoneNumber,
                UserRole.Company
            );

            if (user == null)
                throw new Exception(userError);

            var (profile, profileError) = CompanyProfile.Create(
                userId,
                dto.Name,
                dto.Country,
                dto.Region,
                dto.Settlement,
                dto.ZipCode,
                dto.RegistrationAdress,
                dto.СompanyRegistrationNumber,
                dto.EstimatedAt,
                photoUrl ?? null,
                dto.Description
            );

            if (profile == null)
                throw new Exception(profileError);

            await _userRepository.AddCompanyUserAsync(user, profile);

            return userId;
        }

        public async Task<(Guid Id, string email, UserRole Role, bool isDeleted, bool isTwoFactorEnabled)> VerifyUsersCredentials(string emailOrPhone, string password)
        {
            var existingUser = await _userRepository.FindUserByEmailOrPhoneAsync(emailOrPhone, emailOrPhone);
            var isDeleted = false;

            if (existingUser == null || !_passwordHasher.Verify(password, existingUser.PasswordHash) || existingUser.IsPermanantlyDeleted == true || (existingUser.IsSoftDeleted == true && existingUser.DeletedAt < DateTime.UtcNow.AddMonths(-6)))
            { 
                throw new NotValidCredentialsException();
            }

            if (existingUser.IsSoftDeleted == true && existingUser.DeletedAt > DateTime.UtcNow.AddMonths(-6) && existingUser.IsPermanantlyDeleted != true) 
            {
                isDeleted = true;
            }

            if (existingUser.IsBlocked == true) {
                throw new BlockedUserAccountException();                               
            }

            return (existingUser.Id, existingUser.Email, existingUser.Role, isDeleted, existingUser.IsTwoFactorEnabled);
        }

        public async Task PatchPersonProfileAsync(Guid userId, EditPersonUserDto? updatedProfile, string? newMainPhotoUrl)
        {
            await _userRepository.PatchPersonProfileAsync(userId, updatedProfile?.FirstName, updatedProfile?.LastName, newMainPhotoUrl, updatedProfile?.Country, updatedProfile?.Region, updatedProfile?.Settlement, updatedProfile?.ZipCode);
        }

        public async Task PatchCompanyProfileAsync(Guid userId, EditCompanyUserDto? updatedProfile, string? newMainPhotoUrl)
        {
            await _userRepository.PatchCompanyProfileAsync(userId, updatedProfile?.Name, updatedProfile?.Country, updatedProfile?.Region, updatedProfile?.Settlement, updatedProfile?.ZipCode, updatedProfile?.RegistrationAdress, updatedProfile?.СompanyRegistrationNumber, updatedProfile?.EstimatedAt, newMainPhotoUrl, updatedProfile?.Description);
        }

        public async Task<string?> GetUserMainPhotoUrlByUserId(Guid userId)
        {
            return await _userRepository.GetUserMainPhotoUrlByUserId(userId);
        }

        public async Task SoftDeleteAccount(Guid id)
        {
            await _userRepository.SoftDeleteUserAsync(id);
        }

        public async Task RestoreDeletedAccount(Guid id)
        {
            await _userRepository.RestoreUserAsync(id);
        }

        public async Task PermanantlyDeleteAccount(Guid id)
        {
            await _userRepository.PermanantlyDeleteUserAsync(id);
        }

        public async Task<object?> GetUserProfileInfo(Guid userId, UserRole role)
        {
            var user = await _userRepository.GetUserById(userId);

            if (user == null)
            {
                throw new NotImplementedException();
            }

            if (role == UserRole.Person)
            {
                var profile = await _userRepository.GetPersonUserInfoByIdAsync(userId);

                if (profile == null)
                {
                    throw new NotImplementedException();
                }

                var profileDto = new PersonUserProfileDto
                {
                    FirstName = profile.FirstName,
                    LastName = profile.LastName,
                    Email = user.Email,
                    PhoneNumer = user.PhoneNumber,
                    IsVerified = user.IsVerified,
                    MainPhotoUrl = profile?.MainPhotoUrl,
                    Country = profile?.Country,
                    Region = profile?.Region,
                    Settlement = profile?.Settlement,
                    ZipCode = profile?.ZipCode
                };

                return profileDto;
            }
            else if (role == UserRole.Company)
            {
                var profile = await _userRepository.GetCompanyUserInfoByIdAsync(userId);

                if (profile == null)
                {
                    throw new NotImplementedException();
                }

                var profileDto = new CompanyUserProfileDto
                {
                   Name = profile.Name,
                   Email = user.Email,
                   PhoneNumer = user.PhoneNumber,
                   IsVerified = user.IsVerified,
                   Country = profile.Country,
                   Region = profile.Region,
                   Settlement = profile.Settlement,
                   ZipCode = profile.ZipCode,
                   RegistrationAdress = profile.RegistrationAdress,
                   СompanyRegistrationNumber = profile.СompanyRegistrationNumber,
                   EstimatedAt = profile.EstimatedAt,
                   MainPhotoUrl = profile.MainPhotoUrl,
                   Description = profile.Description,
                };

                return profileDto;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public async Task<Guid?> GetUserIdByEmailAsync(string email)
        {
            var result = await _userRepository.GetUserIdByEmailAsync(email);

            return result;
        }

        public async Task ChangePasswordAsync(Guid userId, string password)
        {
            var hashedPassword = _passwordHasher.Generate(password);

            await _userRepository.PatchUserInfoAsync(userId, passwordHash: hashedPassword);
        }

        public async Task ChangeEmailAsync(Guid userId, string email)
        {
            await _userRepository.PatchUserInfoAsync(userId, email: email);
        }

        public async Task ChangePhoneNumberAsync(Guid userId, string phoneNumber)
        {
            await _userRepository.PatchUserInfoAsync(userId, phoneNumber: phoneNumber);
        }

        public async Task<User?> GetUserById(Guid id)
        {
            var user = await _userRepository.GetUserById(id);

            return user;
        }

        public async Task ChangeUserPasswordWithOldPasswordVerification(Guid userId, string oldPassord, string newPassword)
        {
            var oldPasswordHash = await _userRepository.GetPasswordHashByUserId(userId);
            if (oldPasswordHash == null) throw new Exception();

            if (!_passwordHasher.Verify(oldPassord, oldPasswordHash)) throw new Exception("Provided previous password is incorrect");

            var hashedPassword = _passwordHasher.Generate(newPassword);

            await _userRepository.PatchUserInfoAsync(userId, passwordHash: hashedPassword);
        }

        public async Task SetTwoFactorAuthentication(string userId, bool flag)
        {
            await _userRepository.SetTwoFactorAuthentication(Guid.Parse(userId), flag);
        }

        /// <summary>
        /// ///////////////////
        /// </summary>

        private async Task FindExistingOrResentlyDeletedUser(string email, string phoneNumber)
        {
            var existingUser = await _userRepository.FindUserByEmailOrPhoneAsync(email, phoneNumber);

            if (existingUser != null)
            {
                if (existingUser.DeletedAt.HasValue && existingUser.DeletedAt.Value > DateTime.UtcNow.AddMonths(-6) && existingUser.IsPermanantlyDeleted != true)
                {
                    throw new RecentlyDeletedUserExceptionOnCreating();
                }
                else if(existingUser.IsPermanantlyDeleted != true)
                {
                    throw new UserAlreadyExistsException();
                }
            }

        }
    }
}
