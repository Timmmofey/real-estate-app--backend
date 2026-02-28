using UserService.Domain.Models;
using UserService.Domain.Abstactions;
using UserService.Application.DTOs;
using Classified.Shared.Constants;
using UserService.Application.Abstactions;
using Classified.Shared.Infrastructure.S3.Abstractions;
using Microsoft.AspNetCore.Http;
using Classified.Shared.DTOs;
using Confluent.Kafka;
using UserService.Infrastructure.Kafka;
using Classified.Shared.Infrastructure.RedisService;
using Classified.Shared.Infrastructure.EmailService;
using Newtonsoft.Json;
using UserService.Infrastructure.AuthService;
using UserService.Application.Exeptions;
using UserService.Domain.Consts;

namespace UserService.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserOAuthAccountRepository _userOAuthAccountRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IFileStorageService _fileStorageService;
        private readonly IKafkaProducer _kafkaProducer;
        private readonly IRedisService _redisService;
        private readonly IEmailService _emailService;
        private readonly IAuthServiceClient _authService;
        private readonly IUnitOfWork _unitOfWork;

        public UserService(IUserRepository userRepository, IPasswordHasher passwordHasher, IFileStorageService fileStorageService, IKafkaProducer kafkaProducer, IRedisService redisService, IEmailService emailService, IAuthServiceClient authService, IUnitOfWork unitOfWork, IUserOAuthAccountRepository userOAuthAccountRepository)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _fileStorageService = fileStorageService;
            _kafkaProducer = kafkaProducer;
            _redisService = redisService;
            _emailService = emailService;
            _authService = authService;
            _unitOfWork = unitOfWork;
            _userOAuthAccountRepository = userOAuthAccountRepository;
        }

        public async Task<Guid> CreatePersonUserAsync(CreatePersonUserDto dto)
        {
            await FindExistingOrResentlyDeletedUser(dto.Email, dto.PhoneNumber);

            string? personMainPhotoUrl = await UploadPhotoAsync(dto.MainPhoto, S3FolderName.UserProfileImages);


            var userId = Guid.NewGuid();
            var hashedPassword = _passwordHasher.Generate(dto.Password);

            var (user, userError) = User.CreateNew(
                userId,
                dto.Email,
                dto.PhoneNumber,
                hashedPassword,
                UserRole.Person
            );

            if (user == null)
                throw new Exception(userError);

            var (profile, profileError) = PersonProfile.Create(
                userId,
                dto.FirstName,
                dto.LastName,
                personMainPhotoUrl,
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

        public async Task<Guid> CreateCompanyUserAsync(CreateCompanyUserDto dto)
        {
            await FindExistingOrResentlyDeletedUser(dto.Email, dto.PhoneNumber);

            string? companyMainPhotoUrl = await UploadPhotoAsync(dto.MainPhoto, S3FolderName.UserProfileImages);


            var userId = Guid.NewGuid();
            var hashedPassword = _passwordHasher.Generate(dto.Password);

            var (user, userError) = User.CreateNew(
                userId,
                dto.Email,
                dto.PhoneNumber,
                hashedPassword,
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
                companyMainPhotoUrl,
                dto.Description
            );

            if (profile == null)
                throw new Exception(profileError);

            await _userRepository.AddCompanyUserAsync(user, profile);

            return userId;
        }

        public async Task<Guid> CreatePersonUserFromOAuthAsync(CreatePersonUserOAuthDto dto)
        {
            await FindExistingOrResentlyDeletedUser(dto.Email, dto.PhoneNumber);

            string? personMainPhotoUrl = null;
            if (dto.MainPhoto != null)
                personMainPhotoUrl = await UploadPhotoAsync(dto.MainPhoto, S3FolderName.UserProfileImages);

            var userId = Guid.NewGuid();
            string? hashedPassword = null;

            if (!string.IsNullOrEmpty(dto.Password))
                hashedPassword = _passwordHasher.Generate(dto.Password);

            

            await _unitOfWork.BeginAsync();

            try
            {
                var (user, userError) = User.CreateNew(
                    userId,
                    dto.Email,
                    dto.PhoneNumber,
                    hashedPassword,
                    UserRole.Person
                );

                if (user == null)
                    throw new Exception(userError);

                var (profile, profileError) = PersonProfile.Create(
                    userId,
                    dto!.FirstName,
                    dto.LastName,
                    personMainPhotoUrl,
                    dto?.Country,
                    dto?.Region,
                    dto?.Settlement,
                    dto?.ZipCode
                );

                if (profile == null)
                    throw new Exception(profileError);

                var (oAuthAccount, oAuthAccountError) = UserOAuthAccount.CreateNew(
                    userId,
                    dto!.Provider,
                    dto.ProviderUserId
                );

                if (oAuthAccount == null)
                    throw new Exception(oAuthAccountError);

                await _userRepository.AddUserAsync(user);
                await _userRepository.AddPersonProfileAsync(profile);
                await _userOAuthAccountRepository.AddUserOAuthAccountAsync(oAuthAccount);


                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
                      
            return userId;
        }

        public async Task<Guid> CreateCompanyUserFromOAuthAsync(CreateCompanyUserOAuthDto dto)
        {
            await FindExistingOrResentlyDeletedUser(dto.Email, dto.PhoneNumber);

            string? companyMainPhotoUrl = null;
            if (dto.MainPhoto != null)
                companyMainPhotoUrl = await UploadPhotoAsync(dto.MainPhoto, S3FolderName.UserProfileImages);

            var userId = Guid.NewGuid();
            string? hashedPassword = null;

            if (!string.IsNullOrEmpty(dto.Password))
                hashedPassword = _passwordHasher.Generate(dto.Password);


            await _unitOfWork.BeginAsync();

            try
            {
                var (user, userError) = User.CreateNew(
                    userId,
                    dto.Email,
                    dto.PhoneNumber,
                    hashedPassword,
                    UserRole.Company
                );

                if (user == null)
                    throw new Exception(userError);

                var (profile, profileError) = CompanyProfile.Create(
                    userId,
                    dto.Name,
                    dto.Country ?? throw new Exception("Country is required"),
                    dto.Region ?? throw new Exception("Region is required"),
                    dto.Settlement ?? throw new Exception("Settlement is required"),
                    dto.ZipCode ?? throw new Exception("ZipCode is required"),
                    dto.RegistrationAdress,
                    dto.СompanyRegistrationNumber,
                    dto.EstimatedAt,
                    companyMainPhotoUrl,
                    dto.Description
                );

                if (profile == null)
                    throw new Exception(profileError);

                var (oAuthAccount, oAuthAccountError) = UserOAuthAccount.CreateNew(
                    userId,
                    dto.Provider,
                    dto.ProviderUserId
                );

                if (oAuthAccount == null)
                    throw new Exception(oAuthAccountError);

                await _userRepository.AddUserAsync(user);
                await _userRepository.AddCompanyProfileAsync(profile);
                await _userOAuthAccountRepository.AddUserOAuthAccountAsync(oAuthAccount);


                await _unitOfWork.CommitAsync();
            } 
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }


            return userId;
        }

        public async Task<VerifiedUserDto> VerifyUsersCredentials(string emailOrPhone, string password)
        {
            var existingUser = await _userRepository.FindUserByEmailOrPhoneAsync(emailOrPhone, emailOrPhone);
            var isDeleted = false;

            if (existingUser?.PasswordHash == null)
                throw new UserDoesntHavePasswordException();

            if (existingUser == null || !_passwordHasher.Verify(password, existingUser.PasswordHash) || existingUser.IsPermanantlyDeleted == true || (existingUser.IsSoftDeleted == true && existingUser.DeletedAt < DateTime.UtcNow.AddMonths(-6)))
            { 
                throw new NotValidCredentialsException();
            }

            if (existingUser.IsSoftDeleted == true && existingUser.DeletedAt > DateTime.UtcNow.AddMonths(-6) && existingUser.IsPermanantlyDeleted != true) 
            {
                isDeleted = true;
            }

            return new VerifiedUserDto
            {
                Id = existingUser.Id,
                Role = existingUser.Role,
                Email = existingUser.Email,
                IsDeleted = isDeleted,
                IsTwoFactorEnabled = existingUser.IsTwoFactorEnabled,
                IsBlocked = existingUser.IsBlocked,
            };
        }

        public async Task PatchPersonProfileAsync(Guid userId, EditPersonUserRequest updatedProfile)
        {
            // получаем текущее фото, если будет загрузка или удаление
            string? prevMainPhotoUrl = null;
            if (updatedProfile.MainPhoto != null || updatedProfile.DeleteMainPhoto == true)
            {
                prevMainPhotoUrl = await _userRepository.GetUserMainPhotoUrlByUserId(userId);
            }

            // Подготовим URL нового фото или флаг удаления
            string? newPhotoUrl = null;
            if (updatedProfile.DeleteMainPhoto == true)
            {
                newPhotoUrl = "__DELETE__";
            }
            else if (updatedProfile.MainPhoto != null)
            {
                newPhotoUrl = await UploadPhotoAsync(updatedProfile.MainPhoto, S3FolderName.UserProfileImages);
            }

            // Когда новое фото загружено или удалено — удаляем старое
            if (!string.IsNullOrEmpty(prevMainPhotoUrl))
            {
                await DeletePhotoAsync(prevMainPhotoUrl);
            }

            await _userRepository.PatchPersonProfileAsync(userId, updatedProfile?.FirstName, updatedProfile?.LastName, newPhotoUrl, updatedProfile?.Country, updatedProfile?.Region, updatedProfile?.Settlement, updatedProfile?.ZipCode);
        }


        public async Task PatchCompanyProfileAsync(Guid userId, EditCompanyUserRequest updatedProfile)
        {
            // получаем текущее фото, если будет загрузка или удаление
            string? prevMainPhotoUrl = null;
            if (updatedProfile.MainPhoto != null || updatedProfile.DeleteMainPhoto == true)
            {
                prevMainPhotoUrl = await _userRepository.GetUserMainPhotoUrlByUserId(userId);
            }

            // Подготовим URL нового фото или флаг удаления
            string? newPhotoUrl = null;
            if (updatedProfile.DeleteMainPhoto == true)
            {
                newPhotoUrl = "__DELETE__";
            }
            else if (updatedProfile.MainPhoto != null)
            {
                newPhotoUrl = await UploadPhotoAsync(updatedProfile.MainPhoto, S3FolderName.UserProfileImages);
            }

            // Когда новое фото загружено или удалено — удаляем старое
            if (!string.IsNullOrEmpty(prevMainPhotoUrl))
            {
                await DeletePhotoAsync(prevMainPhotoUrl);
            }

            await _userRepository.PatchCompanyProfileAsync(userId, updatedProfile?.Name, updatedProfile?.Country, updatedProfile?.Region, updatedProfile?.Settlement, updatedProfile?.ZipCode, updatedProfile?.RegistrationAdress, updatedProfile?.СompanyRegistrationNumber, updatedProfile?.EstimatedAt, newPhotoUrl, updatedProfile?.Description);
        }

        public async Task<string?> GetUserMainPhotoUrlByUserId(Guid userId)
        {
            return await _userRepository.GetUserMainPhotoUrlByUserId(userId);
        }

        public async Task SoftDeleteAccount(Guid id)
        {
            try
            {
                await _kafkaProducer.ProduceAsync(KafkaTopic.RecalledSessionsTopic, new Message<string, string>
                {
                    Key = Guid.NewGuid().ToString(),
                    Value = id.ToString()
                });
            }
            catch (InvalidOperationException e)
            {
                throw new Exception(e.Message);
            }

            await _userRepository.SoftDeleteUserAsync(id);
        }

        public async Task RestoreDeletedAccount(Guid id)
        {
            await _userRepository.RestoreUserAsync(id);
        }

        public async Task PermanantlyDeleteAccount(Guid id)
        {
            await _unitOfWork.BeginAsync();
            try
            {
                await _userRepository.PermanantlyDeleteUserAsync(id);
                await _userOAuthAccountRepository.DeleteAllOAuthAccountsByUserIdAsync(id);
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<object?> GetUserProfileInfo(Guid userId, UserRole role)
        {
            var user = await _userRepository.GetUserById(userId);

            if (user == null)
            {
                throw new KeyNotFoundException();
            }

            if (role == UserRole.Person)
            {
                var profile = await _userRepository.GetPersonUserInfoByIdAsync(userId);

                if (profile == null)
                {
                    throw new KeyNotFoundException();
                }

                var profileDto = new PersonUserProfileDto
                {
                    FirstName = profile.FirstName,
                    LastName = profile.LastName,
                    Email = user.Email,
                    PhoneNumer = user.PhoneNumber,
                    IsVerified = user.IsVerified,
                    IsTwoFactorEnabled = user.IsTwoFactorEnabled,
                    IsOAuthOnly = String.IsNullOrEmpty(user.PasswordHash),
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
                    throw new KeyNotFoundException();
                }

                var profileDto = new CompanyUserProfileDto
                {
                    Name = profile.Name,
                    Email = user.Email,
                    PhoneNumer = user.PhoneNumber,
                    IsVerified = user.IsVerified,
                    IsTwoFactorEnabled = user.IsTwoFactorEnabled,
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
                throw new KeyNotFoundException();
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

        public async Task<VerifiedUserDto?> GetVerifiedUserDtoById(Guid id)
        {
            var user = await _userRepository.GetUserById(id);
            var isDeleted = false;

            if (user == null )
            {
                throw new NotValidCredentialsException();
            }

            if (user.IsSoftDeleted == true && user.DeletedAt > DateTime.UtcNow.AddMonths(-6) && user.IsPermanantlyDeleted != true)
            {
                isDeleted = true;
            }

            return new VerifiedUserDto
            {
                Id = user.Id,
                Role = user.Role,
                Email = user.Email,
                IsDeleted = isDeleted,
                IsTwoFactorEnabled = user.IsTwoFactorEnabled,
                IsBlocked = user.IsBlocked,
            };

        }

        public async Task ChangeUserPasswordWithOldPasswordVerification(Guid userId, string oldPassord, string newPassword)
        {
            var oldPasswordHash = await _userRepository.GetPasswordHashByUserId(userId);
            if (oldPasswordHash == null) throw new Exception();

            if (!_passwordHasher.Verify(oldPassord, oldPasswordHash)) throw new Exception("Provided previous password is incorrect");

            var hashedPassword = _passwordHasher.Generate(newPassword);

            await _userRepository.PatchUserInfoAsync(userId, passwordHash: hashedPassword);
        }

        public async Task RequestToggleTwoFactorAuthenticationCode(Guid userId)
        {
            var user = await _userRepository.GetUserById(userId);
            if (string.IsNullOrEmpty(user?.Email))
                throw new Exception("User not found"); ;


            var verificationCode = Guid.NewGuid().ToString().Substring(0, 11).Replace("-", "").ToUpper();

            var redisData = new
            {
                Code = verificationCode,
                UserId = userId
            };

            await _redisService.SetAsync(
                key: $"{RedisKey.ToggleTwoStepAuthCaode}:{userId}",
                value: System.Text.Json.JsonSerializer.Serialize(redisData),
                expiration: TimeSpan.FromMinutes(5)
            );

            await _emailService.SendEmail(user.Email, "Two step authentication confirmation code", verificationCode);
        }

        public async Task ToggleTwoFactorAuthentication(Guid userId, string verificationCode)
        {
            var redisValue = await _redisService.GetAsync($"{RedisKey.ToggleTwoStepAuthCaode}:{userId}");

            if (string.IsNullOrEmpty(redisValue))
                throw new Exception("Verification code expired or not found");

            var redisData = JsonConvert.DeserializeObject<dynamic>(redisValue); ;

            string storedCode = redisData!.Code;

            if (!string.Equals(storedCode, verificationCode, StringComparison.OrdinalIgnoreCase))
                throw new Exception("Invalid verification code");

            await _userRepository.ToggleTwoFactorAuthentication(userId);
        }

        public async Task StartPasswordResetViaEmail(string email)
        {

            var userId = await _userRepository.GetUserIdByEmailAsync(email);

            if (userId is null)
                throw new Exception("User with such email doesn't exist");

            var verificationCode = Guid.NewGuid().ToString().Substring(0, 11).Replace("-", "").ToUpper();

            // Сохраняем verificationCode и userId в Redis
            var redisData = new
            {
                Code = verificationCode,
                UserId = userId
            };

            await _redisService.SetAsync(
                key: $"{RedisKey.PasswordReset}:{email}",
                value: System.Text.Json.JsonSerializer.Serialize(redisData),
                expiration: TimeSpan.FromMinutes(5)
            );

            await _emailService.SendEmail(email, "Password Reset Code", verificationCode);
        }

        public async Task<string> GetPasswordResetTokenViaEmail(GetPasswordResetTokenDto dto)
        {
            var redisValue = await _redisService.GetAsync($"{RedisKey.PasswordReset}:{dto.Email}");

            if (string.IsNullOrEmpty(redisValue))
                throw new  Exception("Verification code expired or not found");

            var redisData = JsonConvert.DeserializeObject<dynamic>(redisValue);

            string storedCode = redisData!.Code;
            Guid userId = redisData.UserId;

            if (!string.Equals(storedCode, dto.VerificationCode, StringComparison.OrdinalIgnoreCase))
                throw new Exception("Invalid verification code");

            var resetPasswordToken = await _authService.getResetPasswordToken(userId);

            if (string.IsNullOrEmpty(resetPasswordToken))
                throw new Exception("Failed to generate reset password token");

            // Удаляем временный код из Redis
            await _redisService.DeleteAsync($"{RedisKey.PasswordReset}:{dto.Email}");

            return resetPasswordToken;
        }

        public async Task startEmailChangeViaEmailViaEmail(Guid userId)
        {
            var user = await _userRepository.GetUserById(userId);
            if (string.IsNullOrEmpty(user?.Email))
                throw new Exception("User not found"); ;

            var emailResetCode = Guid.NewGuid().ToString().Substring(0, 11).Replace("-", "").ToUpper();

            var redisData = new { Code = emailResetCode, UserId = userId };

            await _redisService.SetAsync(
                key: $"{RedisKey.CurrentEmailConfirmationCode}:{userId}",
                value: System.Text.Json.JsonSerializer.Serialize(redisData),
                expiration: TimeSpan.FromMinutes(5)
            );

            await _emailService.SendEmail(user.Email, "Email Reset Code", emailResetCode);
        }

        public async Task<string> getResetEmailToken(Guid userId, string verificationCode) 
        {
            var redisValue = await _redisService.GetAsync($"{RedisKey.CurrentEmailConfirmationCode}:{userId}");

            if (string.IsNullOrEmpty(redisValue))
                throw new Exception("Verification code expired or not found");

            var redisData = JsonConvert.DeserializeObject<dynamic>(redisValue); ;

            string storedCode = redisData!.Code;

            if (!string.Equals(storedCode, verificationCode, StringComparison.OrdinalIgnoreCase))
                throw new Exception("Invalid verification code");

            var resetEmailToken = await _authService.getRequestNewEmailCofirmationToken(userId);

            if (string.IsNullOrEmpty(resetEmailToken))
                throw new Exception("Failed to generate request new email confirmation token");

            await _redisService.DeleteAsync($"{RedisKey.CurrentEmailConfirmationCode}:{userId}");

            return resetEmailToken;
        }

        public async Task sendCofirmationCodeToNewEmail(Guid userId, string email)
        {
            var isEmailTaken = await _userRepository.GetUserIdByEmailAsync(email);

            if (isEmailTaken != null) throw new Exception("this email is taken");

            var newEmailCOfirmationCode = Guid.NewGuid().ToString().Substring(0, 11).Replace("-", "").ToUpper();

            var redisData = new { Code = newEmailCOfirmationCode, Email = email };

            await _redisService.SetAsync(
                key: $"{RedisKey.NewEmailCofirmationCode}:{userId}",
                value: System.Text.Json.JsonSerializer.Serialize(redisData),
                expiration: TimeSpan.FromMinutes(5)
            );

            await _emailService.SendEmail(email, "New email confirmation code", newEmailCOfirmationCode);

        }

        public async Task<string> confirmNewEmail(Guid userId, string verificationCode)
        {
            var redisValue = await _redisService.GetAsync($"{RedisKey.NewEmailCofirmationCode}:{userId}");

            if (string.IsNullOrEmpty(redisValue))
                throw new Exception("Verification code expired or not found");

            var redisData = JsonConvert.DeserializeObject<dynamic>(redisValue);

            string storedCode = redisData!.Code;
            string storedEmail = redisData!.Email;

            if (!string.Equals(storedCode, verificationCode, StringComparison.OrdinalIgnoreCase))
                throw new Exception("Invalid verification code");

            var resetEmailToken = await _authService.getEmailResetToken(userId, storedEmail);

            if (string.IsNullOrEmpty(resetEmailToken))
                throw new Exception("Failed to generate reset password token");

            await _redisService.DeleteAsync($"{RedisKey.NewEmailCofirmationCode}:{userId}");

            return resetEmailToken;
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

        private async Task<string?> UploadPhotoAsync(IFormFile? photo, string folder)
        {
            if (photo == null)
                return null;
            try
            {
                string photoId = Guid.NewGuid().ToString();
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(photo.FileName)}";

                using var stream = photo.OpenReadStream();
                return await _fileStorageService.UploadFileAsync(
                    stream,
                    fileName,
                    folder,
                    photoId,
                    "image");
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        private async Task DeletePhotoAsync(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new Exception("URL cannot be null or empty.");
            }

            try
            {
                await _fileStorageService.DeleteFileAsync(url);
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }
    }
}
