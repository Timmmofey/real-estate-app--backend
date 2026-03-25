using Classified.Shared.Constants;
using Classified.Shared.DTOs;
using Classified.Shared.Extensions.ErrorHandler.Errors;
using Classified.Shared.Infrastructure.EmailService;
using Classified.Shared.Infrastructure.RedisService;
using Classified.Shared.Infrastructure.S3.Abstractions;
using Confluent.Kafka;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using UserService.Application.Abstactions;
using UserService.Application.DTOs;
using UserService.Domain.Abstactions;
using UserService.Domain.Consts;
using UserService.Domain.Models;
using UserService.Infrastructure.AuthService;
using UserService.Infrastructure.GeoService;
using UserService.Infrastructure.Kafka;

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
        private readonly IGeoServiceClient _geoServiceClient;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStringLocalizer<UserService> _localizer;


        public UserService(IUserRepository userRepository, IPasswordHasher passwordHasher, IFileStorageService fileStorageService, IKafkaProducer kafkaProducer, IRedisService redisService, IEmailService emailService, IAuthServiceClient authService, IUnitOfWork unitOfWork, IUserOAuthAccountRepository userOAuthAccountRepository, IGeoServiceClient geoServiceClient, IStringLocalizer<UserService> localizer)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _fileStorageService = fileStorageService;
            _kafkaProducer = kafkaProducer;
            _redisService = redisService;
            _emailService = emailService;
            _authService = authService;
            _unitOfWork = unitOfWork;
            _geoServiceClient = geoServiceClient;
            _userOAuthAccountRepository = userOAuthAccountRepository;
            _localizer = localizer;
        }

        public async Task<Guid> CreatePersonUserAsync(CreatePersonUserDto dto, CancellationToken ct)
        {
            await ValidateEmailAndPhoneNumberUniquenessAsync(dto.Email, dto.PhoneNumber, ct);

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
                null,
                dto.Country,
                dto.Region,
                dto.Settlement,
                dto.ZipCode
            );

            await ValidateAdrress(dto.Country, dto.Region, dto.Settlement);

            if (profile == null)
                throw new NullReferenceException(profileError);

            await _userRepository.AddPersonUserAsync(user, profile, ct);

            if (dto.MainPhoto != null)
            {
                var photoUrl = await UploadPhotoAsync(dto.MainPhoto, S3FolderName.UserProfileImages);

                await _userRepository.PatchPersonProfileAsync(userId, ct, null, null, photoUrl, null, null, null, null);
            }

            return userId;
        }

        public async Task<Guid> CreateCompanyUserAsync(CreateCompanyUserDto dto, CancellationToken ct)
        {
            await ValidateEmailAndPhoneNumberUniquenessAsync(dto.Email, dto.PhoneNumber, ct);

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
                null,
                dto.Description
            );

            if (profile == null)
                throw new Exception(profileError);

            await ValidateAdrress(dto.Country, dto.Region, dto.Settlement);

            await _userRepository.AddCompanyUserAsync(user, profile, ct);

            if (dto.MainPhoto != null)
            {
                var photoUrl = await UploadPhotoAsync( dto.MainPhoto, S3FolderName.UserProfileImages);

                await _userRepository.PatchCompanyProfileAsync(userId, ct, null, null, null, null, null, null, null, null, photoUrl, null);
            }

            return userId;
        }

        public async Task<Guid> CreatePersonUserFromOAuthAsync(CreatePersonUserOAuthDto dto, CancellationToken ct)
        {
            await FindExistingOrResentlyDeletedUser(dto.Email, dto.PhoneNumber, ct);

            var userId = Guid.NewGuid();
            string? hashedPassword = null;

            if (!string.IsNullOrEmpty(dto.Password))
                hashedPassword = _passwordHasher.Generate(dto.Password);

            await ValidateAdrress(dto.Country, dto.Region, dto.Settlement);

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
                    null,
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

                await _userRepository.AddUserAsync(user, ct);
                await _userRepository.AddPersonProfileAsync(profile, ct);
                await _userOAuthAccountRepository.AddUserOAuthAccountAsync(oAuthAccount,ct);


                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }

            if (dto.MainPhoto != null)
            {
                var photoUrl = await UploadPhotoAsync(
                    dto.MainPhoto,
                    S3FolderName.UserProfileImages);

                await _userRepository.PatchPersonProfileAsync(userId,ct, null, null, photoUrl, null, null, null, null);
            }

            return userId;
        }

        public async Task<Guid> CreateCompanyUserFromOAuthAsync(CreateCompanyUserOAuthDto dto, CancellationToken ct)
        {
            await FindExistingOrResentlyDeletedUser(dto.Email, dto.PhoneNumber, ct);

            var userId = Guid.NewGuid();
            string? hashedPassword = null;

            if (!string.IsNullOrEmpty(dto.Password))
                hashedPassword = _passwordHasher.Generate(dto.Password);

            await ValidateAdrress(dto.Country, dto.Region, dto.Settlement);

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
                    dto.Country ?? throw new ArgumentException("Country is required"),
                    dto.Region ?? throw new ArgumentException("Region is required"),
                    dto.Settlement ?? throw new ArgumentException("Settlement is required"),
                    dto.ZipCode ?? throw new ArgumentException("ZipCode is required"),
                    dto.RegistrationAdress,
                    dto.СompanyRegistrationNumber,
                    dto.EstimatedAt,
                    null,
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

                await _userRepository.AddUserAsync(user, ct);
                await _userRepository.AddCompanyProfileAsync(profile, ct);
                await _userOAuthAccountRepository.AddUserOAuthAccountAsync(oAuthAccount,ct);


                await _unitOfWork.CommitAsync();
            } 
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }

            if (dto.MainPhoto != null)
            {
                var photoUrl = await UploadPhotoAsync(
                    dto.MainPhoto,
                    S3FolderName.UserProfileImages);

                await _userRepository.PatchCompanyProfileAsync(userId, ct, null, null, null, null, null, null, null, null, photoUrl, null);
            }

            return userId;
        }

        public async Task<VerifiedUserDto> VerifyUsersCredentials(string emailOrPhone, string password, CancellationToken ct)
        {
            var existingUser = await _userRepository.FindUserByEmailOrPhoneAsync(ct, emailOrPhone, emailOrPhone);
            var isDeleted = false;

            if (existingUser?.PasswordHash == null)
                throw new NullReferenceException();

            if (existingUser == null || !_passwordHasher.Verify(password, existingUser.PasswordHash) || existingUser.IsPermanantlyDeleted == true || (existingUser.IsSoftDeleted == true && existingUser.DeletedAt < DateTime.UtcNow.AddMonths(-6)))
            { 
                throw new UnauthorizedAccessException("Provided credentials are not valid.");
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

        public async Task PatchPersonProfileAsync(Guid userId, EditPersonUserRequest updatedProfile, CancellationToken ct)
        {
            // получаем текущее фото, если будет загрузка или удаление
            string? prevMainPhotoUrl = null;
            if (updatedProfile.MainPhoto != null || updatedProfile.DeleteMainPhoto == true)
            {
                prevMainPhotoUrl = await _userRepository.GetUserMainPhotoUrlByUserId(userId, ct);
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

            await ValidateAdrress(updatedProfile.Country, updatedProfile.Region, updatedProfile.Settlement);


            await _userRepository.PatchPersonProfileAsync(userId, ct, updatedProfile?.FirstName, updatedProfile?.LastName, newPhotoUrl, updatedProfile?.Country, updatedProfile?.Region, updatedProfile?.Settlement, updatedProfile?.ZipCode);
        }


        public async Task PatchCompanyProfileAsync(Guid userId, EditCompanyUserRequest updatedProfile, CancellationToken ct)
        {
            // получаем текущее фото, если будет загрузка или удаление
            string? prevMainPhotoUrl = null;
            if (updatedProfile.MainPhoto != null || updatedProfile.DeleteMainPhoto == true)
            {
                prevMainPhotoUrl = await _userRepository.GetUserMainPhotoUrlByUserId(userId, ct);
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

            await ValidateAdrress(updatedProfile.Country, updatedProfile.Region, updatedProfile.Settlement);

            await _userRepository.PatchCompanyProfileAsync(userId, ct, updatedProfile?.Name, updatedProfile?.Country, updatedProfile?.Region, updatedProfile?.Settlement, updatedProfile?.ZipCode, updatedProfile?.RegistrationAdress, updatedProfile?.СompanyRegistrationNumber, updatedProfile?.EstimatedAt, newPhotoUrl, updatedProfile?.Description);
        }

        public async Task<string?> GetUserMainPhotoUrlByUserId(Guid userId, CancellationToken ct)
        {
            return await _userRepository.GetUserMainPhotoUrlByUserId(userId, ct);
        }

        public async Task SoftDeleteAccount(Guid id, CancellationToken ct)
        {
            await _kafkaProducer.ProduceAsync(KafkaTopic.RecalledSessionsTopic, new Message<string, string>
            {
                Key = Guid.NewGuid().ToString(),
                Value = id.ToString()
            });

            await _userRepository.SoftDeleteUserAsync(id, ct);
        }

        public async Task RestoreDeletedAccount(Guid id, CancellationToken ct)
        {
            await _userRepository.RestoreUserAsync(id, ct);
        }

        public async Task PermanantlyDeleteAccount(Guid id, CancellationToken ct)
        {
            await _unitOfWork.BeginAsync();
            try
            {
                await _userRepository.PermanantlyDeleteUserAsync(id, ct);
                await _userOAuthAccountRepository.DeleteAllOAuthAccountsByUserIdAsync(id, ct);
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<object?> GetUserProfileInfo(Guid userId, UserRole role, CancellationToken ct)
        {
            var user = await _userRepository.GetUserById(userId, ct);

            if (user == null)
            {
                throw new KeyNotFoundException();
            }

            if (role == UserRole.Person)
            {
                var profile = await _userRepository.GetPersonUserInfoByIdAsync(userId, ct);

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
                var profile = await _userRepository.GetCompanyUserInfoByIdAsync(userId, ct);

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

        public async Task<Guid?> GetUserIdByEmailAsync(string email, CancellationToken ct)
        {
            var result = await _userRepository.GetUserIdByEmailAsync(email, ct);

            return result;
        }

        public async Task ChangePasswordAsync(Guid userId, string password, CancellationToken ct)
        {
            var hashedPassword = _passwordHasher.Generate(password);

            await _userRepository.PatchUserInfoAsync(userId, ct, passwordHash: hashedPassword);
        }

        public async Task ChangeEmailAsync(Guid userId, string email, CancellationToken ct)
        {
            await _userRepository.PatchUserInfoAsync(userId, ct, email: email);
        }

        public async Task ChangePhoneNumberAsync(Guid userId, string phoneNumber, CancellationToken ct)
        {
            await _userRepository.PatchUserInfoAsync(userId, ct, phoneNumber: phoneNumber);
        }

        public async Task<User?> GetUserById(Guid id, CancellationToken ct)
        {
            var user = await _userRepository.GetUserById(id, ct);

            return user;
        }

        public async Task<VerifiedUserDto?> GetVerifiedUserDtoById(Guid id, CancellationToken ct)
        {
            var user = await _userRepository.GetUserById(id, ct);
            var isDeleted = false;

            if (user == null )
            {
                throw new KeyNotFoundException();
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

        public async Task ChangeUserPasswordWithOldPasswordVerification(Guid userId, string oldPassord, string newPassword, CancellationToken ct)
        {
            var oldPasswordHash = await _userRepository.GetPasswordHashByUserId(userId, ct);
            if (oldPasswordHash == null) throw new Exception();

            if (!_passwordHasher.Verify(oldPassord, oldPasswordHash)) throw new DomainValidationException("Provided previous password is incorrect");

            var hashedPassword = _passwordHasher.Generate(newPassword);

            await _userRepository.PatchUserInfoAsync(userId, ct, passwordHash: hashedPassword);
        }

        public async Task RequestToggleTwoFactorAuthenticationCode(Guid userId, CancellationToken ct)
        {
            var user = await _userRepository.GetUserById(userId, ct);
            if (string.IsNullOrEmpty(user?.Email))
                throw new KeyNotFoundException("User not found");

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

        public async Task ToggleTwoFactorAuthentication(Guid userId, string verificationCode, CancellationToken ct)
        {
            var redisValue = await _redisService.GetAsync($"{RedisKey.ToggleTwoStepAuthCaode}:{userId}");

            if (string.IsNullOrEmpty(redisValue))
                throw new NotFoundException("Verification code expired or not found");

            var redisData = JsonConvert.DeserializeObject<dynamic>(redisValue); ;

            string storedCode = redisData!.Code;

            if (!string.Equals(storedCode, verificationCode, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Invalid verification code");

            await _userRepository.ToggleTwoFactorAuthentication(userId, ct);
        }

        public async Task StartPasswordResetViaEmail(string email, CancellationToken ct)
        {

            var userId = await _userRepository.GetUserIdByEmailAsync(email, ct);

            if (userId is null)
                throw new NotFoundException("User with such email doesn't exist");

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

        public async Task<string> GetPasswordResetTokenViaEmail(GetPasswordResetTokenDto dto, CancellationToken ct)
        {
            var redisValue = await _redisService.GetAsync($"{RedisKey.PasswordReset}:{dto.Email}");

            if (string.IsNullOrEmpty(redisValue))
                throw new  NotFoundException("Verification code expired or not found");

            var redisData = JsonConvert.DeserializeObject<dynamic>(redisValue);

            string storedCode = redisData!.Code;
            Guid userId = redisData.UserId;

            if (!string.Equals(storedCode, dto.VerificationCode, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Invalid verification code");

            var resetPasswordToken = await _authService.GetResetPasswordTokenAsync(userId, ct);

            if (string.IsNullOrEmpty(resetPasswordToken))
                throw new Exception("Failed to generate reset password token");

            // Удаляем временный код из Redis
            await _redisService.DeleteAsync($"{RedisKey.PasswordReset}:{dto.Email}");

            return resetPasswordToken;
        }

        public async Task StartEmailChangeViaEmailViaEmail(Guid userId, CancellationToken ct)
        {
            var user = await _userRepository.GetUserById(userId, ct);
            if (string.IsNullOrEmpty(user?.Email))
                throw new KeyNotFoundException("User not found"); ;

            var emailResetCode = Guid.NewGuid().ToString().Substring(0, 11).Replace("-", "").ToUpper();

            var redisData = new { Code = emailResetCode, UserId = userId };

            await _redisService.SetAsync(
                key: $"{RedisKey.CurrentEmailConfirmationCode}:{userId}",
                value: System.Text.Json.JsonSerializer.Serialize(redisData),
                expiration: TimeSpan.FromMinutes(5)
            );

            await _emailService.SendEmail(user.Email, "Email Reset Code", emailResetCode);
        }

        public async Task<string> GetResetEmailToken(Guid userId, string verificationCode, CancellationToken ct) 
        {
            var redisValue = await _redisService.GetAsync($"{RedisKey.CurrentEmailConfirmationCode}:{userId}");

            if (string.IsNullOrEmpty(redisValue))
                throw new NotFoundException("Verification code expired or not found");

            var redisData = JsonConvert.DeserializeObject<dynamic>(redisValue); ;

            string storedCode = redisData!.Code;

            if (!string.Equals(storedCode, verificationCode, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Invalid verification code");

            var resetEmailToken = await _authService.GetRequestNewEmailCofirmationTokenAsync(userId, ct);

            if (string.IsNullOrEmpty(resetEmailToken))
                throw new Exception("Failed to generate request new email confirmation token");

            await _redisService.DeleteAsync($"{RedisKey.CurrentEmailConfirmationCode}:{userId}");

            return resetEmailToken;
        }

        public async Task SendCofirmationCodeToNewEmail(Guid userId, string email, CancellationToken ct)
        {
            var isEmailTaken = await _userRepository.GetUserIdByEmailAsync(email, ct);

            if (isEmailTaken != null) throw new DomainValidationException("this email is taken");

            var newEmailCOfirmationCode = Guid.NewGuid().ToString().Substring(0, 11).Replace("-", "").ToUpper();

            var redisData = new { Code = newEmailCOfirmationCode, Email = email };

            await _redisService.SetAsync(
                key: $"{RedisKey.NewEmailCofirmationCode}:{userId}",
                value: System.Text.Json.JsonSerializer.Serialize(redisData),
                expiration: TimeSpan.FromMinutes(5)
            );

            await _emailService.SendEmail(email, "New email confirmation code", newEmailCOfirmationCode);

        }

        public async Task<string> ConfirmNewEmail(Guid userId, string verificationCode, CancellationToken ct)
        {
            var redisValue = await _redisService.GetAsync($"{RedisKey.NewEmailCofirmationCode}:{userId}");

            if (string.IsNullOrEmpty(redisValue))
                throw new NotFoundException("Verification code expired or not found");

            var redisData = JsonConvert.DeserializeObject<dynamic>(redisValue);

            string storedCode = redisData!.Code;
            string storedEmail = redisData!.Email;

            if (!string.Equals(storedCode, verificationCode, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Invalid verification code");

            var resetEmailToken = await _authService.GetEmailResetTokenAsync(userId, storedEmail, ct);

            if (string.IsNullOrEmpty(resetEmailToken))
                throw new Exception("Failed to generate reset password token");

            await _redisService.DeleteAsync($"{RedisKey.NewEmailCofirmationCode}:{userId}");

            return resetEmailToken;
        }



        /// <summary>
        /// ///////////////////
        /// </summary>



        private async Task FindExistingOrResentlyDeletedUser(string email, string phoneNumber, CancellationToken ct)
        {
            var existingUser = await _userRepository.FindUserByEmailOrPhoneAsync(ct, email, phoneNumber);

            if (existingUser != null)
            {
                if (existingUser.DeletedAt.HasValue && existingUser.DeletedAt.Value > DateTime.UtcNow.AddMonths(-6) && existingUser.IsPermanantlyDeleted != true)
                {
                    throw new DomainValidationException(_localizer["RecentlyDeletedUser"]);
                }
                else if(existingUser.IsPermanantlyDeleted != true)
                {
                    throw new DomainValidationException(_localizer["UserAlreadyExists"]);
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
                throw new ArgumentException("URL cannot be null or empty.");
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

        private async Task ValidateAdrress(string? country = null, string? region = null, string? settlement = null)
        {
            if (country != null && country != "__DELETE__" && country != "none" && (region == null || region != "__DELETE__") && (settlement == null && settlement != "__DELETE__") && !RegionMaps.IsCountryAllowed(country))
                throw new DomainValidationException("Not valid country");

            if (country != null && country != "__DELETE__" && country != "none" && region != null && region != "__DELETE__" && region != "none" && !RegionMaps.IsRegionAllowed(country, region))
                throw new DomainValidationException("Not valid region");

            if (country != null && country != "__DELETE__" && country != "none" && region != null && region != "__DELETE__" && region != "none" && settlement != null && settlement != "__DELETE__")
            {
                var res = await _geoServiceClient.ValidateSettlement(country, region, settlement);

                if (res == false)
                    throw new DomainValidationException("Not valid settlement");
            }
        }

        private async Task ValidateEmailAndPhoneNumberUniquenessAsync(string email, string phoneNumber, CancellationToken ct)
        {
            var errors = new ValidationErrors();

            var existingUserByEmail =
                await _userRepository.GetUserByEmail(email, ct);

            var existingUserByPhone =
                await _userRepository.GetUserByPhoneNumber(phoneNumber, ct);

            if (existingUserByEmail != null)
            {
                errors.Add(
                    nameof(CreateUserBaseAbstract.Email),
                    _localizer["UserWithSuchEmailAlreadyExists"]
                );
            }

            if (existingUserByPhone != null)
            {
                errors.Add(
                    nameof(CreateUserBaseAbstract.PhoneNumber),
                    _localizer["UserWithSuchPhoneNumberAlreadyExists"]
                );
            }

            if (errors.Any())
            {
                throw new DomainValidationException(
                    errors.Errors,
                    _localizer["ErrorWhileUserCreating"]
                );
            }
        }
    }
}
