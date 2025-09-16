﻿using UserService.Domain.Models;
using UserService.Domain.Abstactions;
using UserService.Application.DTOs;
using UserService.Domain.Exeptions;
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


namespace UserService.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IFileStorageService _fileStorageService;
        private readonly IKafkaProducer _kafkaProducer;
        private readonly IRedisService _redisService;
        private readonly IEmailService _emailService;
        private readonly IAuthServiceClient _authService;

        public UserService(IUserRepository userRepository, IPasswordHasher passwordHasher, IFileStorageService fileStorageService, IKafkaProducer kafkaProducer, IRedisService redisService, IEmailService emailService, IAuthServiceClient authService)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _fileStorageService = fileStorageService;
            _kafkaProducer = kafkaProducer;
            _redisService = redisService;
            _emailService = emailService;
            _authService = authService;
        }

        public async Task<Guid> CreatePersonUserAsync(CreatePersonUserDto dto)
        {
            await FindExistingOrResentlyDeletedUser(dto.Email, dto.PhoneNumber);

            string? personMainPhotoUrl = await UploadPhotoAsync(dto.MainPhoto, "userProfileImages");


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

            string? companyMainPhotoUrl = await UploadPhotoAsync(dto.MainPhoto, "userProfileImages");


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
                companyMainPhotoUrl,
                dto.Description
            );

            if (profile == null)
                throw new Exception(profileError);

            await _userRepository.AddCompanyUserAsync(user, profile);

            return userId;
        }

        public async Task<VerifiedUserDto> VerifyUsersCredentials(string emailOrPhone, string password)
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

            return new VerifiedUserDto
            {
                Id = existingUser.Id,
                Role = existingUser.Role,
                Email = existingUser.Email,
                IsDeleted = isDeleted,
                IsTwoFactorEnabled = existingUser.IsTwoFactorEnabled
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
                newPhotoUrl = await UploadPhotoAsync(updatedProfile.MainPhoto, "userProfileImages");
            }

            // Когда новое фото загружено или удалено — удаляем старое
            if (!string.IsNullOrEmpty(prevMainPhotoUrl))
            {
                await DeletePhotoAsync(prevMainPhotoUrl);
            }

            // Формируем DTO без фото
            var updatedFields = new EditPersonUserDto
            {
                FirstName = updatedProfile.FirstName,
                LastName = updatedProfile.LastName,
                Country = updatedProfile.Country,
                Region = updatedProfile.Region,
                Settlement = updatedProfile.Settlement,
                ZipCode = updatedProfile.ZipCode,
            };

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
                newPhotoUrl = await UploadPhotoAsync(updatedProfile.MainPhoto, "userProfileImages");
            }

            // Когда новое фото загружено или удалено — удаляем старое
            if (!string.IsNullOrEmpty(prevMainPhotoUrl))
            {
                await DeletePhotoAsync(prevMainPhotoUrl);
            }

            var updatedFields = new EditCompanyUserDto
            {
                Name = updatedProfile.Name,
                Country = updatedProfile.Country,
                Region = updatedProfile.Region,
                Settlement = updatedProfile.Settlement,
                ZipCode = updatedProfile.ZipCode,
                RegistrationAdress = updatedProfile.RegistrationAdress,
                СompanyRegistrationNumber = updatedProfile.СompanyRegistrationNumber,
                EstimatedAt = updatedProfile.EstimatedAt,
                Description = updatedProfile.Description
            };

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
                await _kafkaProducer.ProduceAsync("recalled-sessions-topic", new Message<string, string>
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

        public async Task StartPasswordResetViaEmail(string email)
        {

            var userId = await _userRepository.GetUserIdByEmailAsync(email);

            if (userId is null)
                throw new Exception("User with such email doesn't exist");

            var passwordResetCode = Guid.NewGuid().ToString().Substring(0, 11).Replace("-", "").ToUpper();

            // Сохраняем verificationCode и userId в Redis
            var redisData = new
            {
                Code = passwordResetCode,
                UserId = userId
            };

            await _redisService.SetAsync(
                key: $"pwd-reset:{email}",
                value: System.Text.Json.JsonSerializer.Serialize(redisData),
                expiration: TimeSpan.FromMinutes(5)
            );

            await _emailService.SendEmail(email, "Password Reset Code", passwordResetCode);
        }

        public async Task<string> GetPasswordResetTokenViaEmail(GetPasswordResetTokenDto dto)
        {
            var redisValue = await _redisService.GetAsync($"pwd-reset:{dto.Email}");

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
            await _redisService.DeleteAsync($"pwd-reset:{dto.Email}");

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
                key: $"current-email-cofirmation-code:{userId}",
                value: System.Text.Json.JsonSerializer.Serialize(redisData),
                expiration: TimeSpan.FromMinutes(5)
            );

            await _emailService.SendEmail(user.Email, "Email Reset Code", emailResetCode);
        }

        public async Task<string> getResetEmailToken(Guid userId, string verificationCode) 
        {
            var redisValue = await _redisService.GetAsync($"current-email-cofirmation-code:{userId}");

            if (string.IsNullOrEmpty(redisValue))
                throw new Exception("Verification code expired or not found");

            var redisData = JsonConvert.DeserializeObject<dynamic>(redisValue); ;

            string storedCode = redisData!.Code;

            if (!string.Equals(storedCode, verificationCode, StringComparison.OrdinalIgnoreCase))
                throw new Exception("Invalid verification code");

            var resetEmailToken = await _authService.getRequestNewEmailCofirmationToken(userId);

            if (string.IsNullOrEmpty(resetEmailToken))
                throw new Exception("Failed to generate request new email confirmation token");

            await _redisService.DeleteAsync($"old-password-cofirmation-code:{userId}");

            return resetEmailToken;
        }

        public async Task sendCofirmationCodeToNewEmail(Guid userId, string email)
        {
            var isEmailTaken = await _userRepository.GetUserIdByEmailAsync(email);

            if (isEmailTaken != null) throw new Exception("this email is taken");

            var newEmailCOfirmationCode = Guid.NewGuid().ToString().Substring(0, 11).Replace("-", "").ToUpper();

            var redisData = new { Code = newEmailCOfirmationCode, Email = email };

            await _redisService.SetAsync(
                key: $"new-email-cofirmation-code:{userId}",
                value: System.Text.Json.JsonSerializer.Serialize(redisData),
                expiration: TimeSpan.FromMinutes(5)
            );

            await _emailService.SendEmail(email, "New email confirmation code", newEmailCOfirmationCode);

        }

        public async Task<string> confirmNewEmail(Guid userId, string verificationCode)
        {
            var redisValue = await _redisService.GetAsync($"new-email-cofirmation-code:{userId}");

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

            await _redisService.DeleteAsync($"new-email-cofirmation-code:{userId}");

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
