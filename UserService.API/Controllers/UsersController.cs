using Microsoft.AspNetCore.Mvc;
using Classified.Shared.Infrastructure.S3.Abstractions;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using UserService.Infrastructure.Kafka;
using Confluent.Kafka;
using System.Security.Claims;
using Classified.Shared.Constants;
using UserService.Application.DTOs;
using UserService.Application.Abstactions;
using UserService.Infrastructure.AuthService;
using Newtonsoft.Json;
using Classified.Shared.Infrastructure.EmailService;
using Microsoft.AspNetCore.Cors;
using Classified.Shared.Infrastructure.RedisService;

namespace UserService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IFileStorageService _fileStorageService;
        private readonly IKafkaProducer _kafkaProducer;
        private readonly IEmailService _emailService;
        private readonly IAuthServiceClient _authService;
        private readonly IRedisService _redisService;

        public UsersController(IUserService userService, IFileStorageService fileStorageService, IKafkaProducer kafkaProducer, IEmailService emailService, IAuthServiceClient authService, IRedisService redisService)
        {
            _userService = userService;
            _fileStorageService = fileStorageService;
            _kafkaProducer = kafkaProducer;
            _emailService = emailService;
            _authService = authService;
            _redisService = redisService;
        }

        [HttpPost("add-person-user")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreatePersonUser([FromForm] CreatePersonUserDto dto)
        {
            string? personMainPhotoUrl = null;

            if (dto.MainPhoto != null)
            {
                try
                {
                    personMainPhotoUrl = await UploadPhotoAsync(dto.MainPhoto, "userProfileImages");
                }
                catch
                {

                }
            }

            try
            {
                var userId = await _userService.CreatePersonUserAsync(dto, personMainPhotoUrl);
                return Created($"/users/{userId}", new { Message = "User has been created successfully", UserId = userId });
            }
            catch (InvalidOperationException e)
            {
                return NotFound(e.Message);
            }
        }

        [HttpPost("add-company-user")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateCompanyUser([FromForm] CreateCompanyUserDto dto)
        {
            string? companyMainPhotoUrl = null;
            if (dto.MainPhoto != null)
            {
                try
                {
                    companyMainPhotoUrl = await UploadPhotoAsync(dto.MainPhoto, "userProfileImages");
                }
                catch
                {

                }
            }

            try
            {

                var userId = await _userService.CreateCompanyUserAsync(dto, companyMainPhotoUrl);
                return Created($"/users/{userId}", new { Message = "User has been created successfully", UserId = userId });
            }
            catch (InvalidOperationException e)
            {
                return NotFound(e.Message);
            }
        }

        [HttpPost("verify-user-credentials")]
        public async Task<IActionResult> VerifyUserCredentials(string phoneOrEmail, string password)
        {
            try
            {
                var (id, role, isDeleted) = await _userService.VerifyUsersCredentials(phoneOrEmail, password);

                return Ok(new VerifiedUserResponseDto
                {
                    Id = id,
                    Role = role,
                    IsDeleted = isDeleted
                });
            }
            catch (InvalidOperationException e)
            {
                return NotFound(e.Message);
            }

        }

        [Authorize(Roles = "Person")]
        [HttpPatch("edit-person-profile-main-info")]
        public async Task<IActionResult> PatchPersonProfile([FromForm] EditPersonUserRequest updatedProfile)
        {
            var userIdClaim = User.Claims.FirstOrDefault(r => r.Type == "userId")?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            // получаем текущее фото, если будет загрузка или удаление
            string? prevMainPhotoUrl = null;
            if (updatedProfile.MainPhoto != null || updatedProfile.DeleteMainPhoto == true)
            {
                prevMainPhotoUrl = await _userService.GetUserMainPhotoUrlByUserId(userId);
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

            try
            {
                // Сохраняем все вместе: поля + новое фото или флаг удаления
                await _userService.PatchPersonProfileAsync(userId, updatedFields, newPhotoUrl);
                return NoContent();
            }
            catch (InvalidOperationException e)
            {
                return NotFound(e.Message);
            }
        }


        [Authorize(Roles = "Company")]
        [HttpPatch("edit-company-profile-main-info")]
        public async Task<IActionResult> PatchCompanyProfile([FromForm] EditCompanyUserRequest updatedProfile)
        {
            var userIdClaim = User.Claims.FirstOrDefault(r => r.Type == "userId")?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            string? companyMainPhotoUrl = null;
            string? prevMainPhotoUrl = null;

            if (updatedProfile.MainPhoto != null && updatedProfile.DeleteMainPhoto != true)
            {
                prevMainPhotoUrl = await _userService.GetUserMainPhotoUrlByUserId(userId);
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

            try
            {
                if (!string.IsNullOrEmpty(prevMainPhotoUrl))
                {
                    companyMainPhotoUrl = await UploadPhotoAsync(updatedProfile.MainPhoto, "userProfileImages");
                    await DeletePhotoAsync(prevMainPhotoUrl);
                }

                if (updatedProfile.DeleteMainPhoto == true)
                {
                    companyMainPhotoUrl = "__DELETE__";
                    await DeletePhotoAsync(prevMainPhotoUrl!);
                }

                await _userService.PatchCompanyProfileAsync(userId, updatedFields, companyMainPhotoUrl);
                return NoContent();
            }
            catch (InvalidOperationException e)
            {
                throw new Exception(e.Message);
            }
        }

        [Authorize(Roles = "Person, Company")]
        [HttpDelete("delete-account")]
        public async Task<IActionResult> DeleteAccount()
        {
            var userIdClaim = User.Claims.FirstOrDefault(r => r.Type == "userId")?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            try
            {
                await _userService.SoftDeleteAccount(userId);
                await _kafkaProducer.ProduceAsync("recalled-sessions-topic", new Message<string, string>
                {
                    Key = Guid.NewGuid().ToString(),
                    Value = userIdClaim
                });
            }
            catch (InvalidOperationException e) { 
                throw new Exception(e.Message);
            }          

            return NoContent();
        }

        [HttpPost("restore-deleted-account")]
        public async Task<IActionResult> RestoreDeletedAccount()
        {
            if (!Request.Cookies.TryGetValue("classified-restore-token", out var tokenHeader))
            {
                return Unauthorized("Missing device token.");
            }

            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(tokenHeader.ToString());

            var idClaim = jwt.Claims.FirstOrDefault(c => c.Type == "Id")?.Value;
            if (!Guid.TryParse(idClaim, out var userId))
            {
                return Unauthorized("Invalid or missing Id claim in token.");
            } 

            await _userService.RestoreDeletedAccount(userId);
            return NoContent();
        }

        [HttpDelete("permanantly-delete-account")]
        public async Task<IActionResult> PermanantlyDeleteAccount()
        {
         
                if (!Request.Cookies.TryGetValue("classified-restore-token", out var tokenHeader))
                {
                    return Unauthorized("Missing device token.");
                }

                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(tokenHeader.ToString());

                var idClaim = jwt.Claims.FirstOrDefault(c => c.Type == "Id")?.Value;
                if (!Guid.TryParse(idClaim, out var userId))
                {
                    return Unauthorized("Invalid or missing Id claim in token.");
                }

            try
            {
                await _userService.PermanantlyDeleteAccount(userId);
                return NoContent();
            }
            catch (InvalidOperationException e)
            {
                throw new Exception(e.Message);
            }
        }

        [HttpGet("get-users-info")]
        public async Task<IActionResult> GetPersonalInfo()
        {
            
                var userIdClaim = User.Claims.FirstOrDefault(r => r.Type == "userId")?.Value;
                var roleClaim = User.Claims.FirstOrDefault(r => r.Type == ClaimTypes.Role)?.Value;

                if (!Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized();
                }

                if (!Enum.TryParse<UserRole>(roleClaim, out var userRole))
                {
                    return Unauthorized();
                }

            try
            {
                var profile = await _userService.GetUserProfileInfo(userId, userRole);

                return Ok(profile);
            }
            catch (InvalidOperationException e)
            {
                throw new Exception(e.Message);
            }
        }

        /// <summary>
        /// /////// Reset Password Via Email
        /// </summary> 

        [HttpPost("start-password-reset-via-email")]
        public async Task<IActionResult> StartPasswordResetViaEmail([FromForm] string email)
        {
            
                if (string.IsNullOrWhiteSpace(email))
                    return BadRequest("Email is required");
            try
            {
                var userId = await _userService.GetUserIdByEmailAsync(email);

                if (userId is null)
                    return BadRequest("User with such email doesn't exist");

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

                return Ok("Reset code sent");
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        //[EnableCors("AllowAuthService")]
        [HttpPost("get-password-reset-token-via-email")]
        public async Task<IActionResult> GetPasswordResetTokenViaEmail([FromBody] GetPasswordResetTokenDto dto)
        {
            
                if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.VerificationCode))
                    return BadRequest("Email and verification code are required");
           
            try
            {
                // Получаем данные из Redis
                var redisValue = await _redisService.GetAsync($"pwd-reset:{dto.Email}");

                if (string.IsNullOrEmpty(redisValue))
                    return BadRequest("Verification code expired or not found");

                var redisData = JsonConvert.DeserializeObject<dynamic>(redisValue); ;

                string storedCode = redisData!.Code;
                Guid userId = redisData.UserId;

                if (!string.Equals(storedCode, dto.VerificationCode, StringComparison.OrdinalIgnoreCase))
                    return BadRequest("Invalid verification code");

                var resetPasswordToken = await _authService.getResetPasswordToken(userId);

                if (string.IsNullOrEmpty(resetPasswordToken))
                    return StatusCode(500, "Failed to generate reset password token");

                // Удаляем временный код из Redis
                await _redisService.DeleteAsync($"pwd-reset:{dto.Email}");

                Response.Cookies.Append("classified-password-reset-token", resetPasswordToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = false, // Поставьте true, если используется HTTPS
                    Expires = DateTimeOffset.UtcNow.AddMinutes(5)
                });

                return Ok("Reset token issued");
            }
            catch (Exception e) 
            {
                throw new Exception(e.Message);
            }
        }

        [HttpPost("complete-password-restoration-via-email")]
        public async Task<IActionResult> CompletePasswordResorationViaEmail([FromForm] string newPassword)
        {
            if (!Request.Cookies.TryGetValue("classified-password-reset-token", out var resetPasswordToken) || string.IsNullOrEmpty(resetPasswordToken))
                return Unauthorized("Refresh token is missing or invalid.");
            
            var handler = new JwtSecurityTokenHandler();

            var resetPasswordJwt = handler.ReadJwtToken(resetPasswordToken);
            var UserIdClaim = resetPasswordJwt.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
            var tokenTypeClaim = resetPasswordJwt.Claims.FirstOrDefault(c => c.Type == "type")?.Value;

            if (tokenTypeClaim != JwtTokenType.PasswordReset.ToString())
                return Forbid();

            try
            {
                await _userService.ChangePasswordAsync(Guid.Parse(UserIdClaim!), newPassword);
                return Ok();
            }
            catch (Exception e) 
            {
                throw new Exception(e.Message);
            }

        }

        /// <summary>
        /// /////// Email Change Via Email
        /// </summary>      

        [HttpPost("start-email-change-via-email")]
        public async Task<IActionResult> startEmailChangeViaEmailViaEmail()
        {
            var userIdClaim = User.Claims.FirstOrDefault(r => r.Type == "userId")?.Value;
            var tokenTypeClaim = User.Claims.FirstOrDefault(c => c.Type == "type")?.Value;

            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            if (tokenTypeClaim != JwtTokenType.Access.ToString())
                return Forbid();

            var currentEmail = await _userService.GetUserEmailById(userId);
            if (string.IsNullOrEmpty(currentEmail))
                return NotFound();

            try
            {
                var emailResetCode = Guid.NewGuid().ToString().Substring(0, 11).Replace("-", "").ToUpper();

                var redisData = new { Code = emailResetCode };

                await _redisService.SetAsync(
                    key: $"email-reset:{userIdClaim}",
                    value: System.Text.Json.JsonSerializer.Serialize(redisData),
                    expiration: TimeSpan.FromMinutes(5)
                );

                await _emailService.SendEmail(currentEmail, "Email Reset Code", emailResetCode);

                return Ok("Reset code sent");
            }
            catch (InvalidOperationException e)
            {
                throw new Exception(e.Message);
            }
        }

        [EnableCors("AllowAuthService")]
        [HttpGet("get-email-reset-token-via-email")]
        public async Task<IActionResult> getEmailResetTokenViaEmailViaEmail([FromForm] GetEmailResetTokenViaEmailDto dto)
        {
            
                var userIdClaim = User.Claims.FirstOrDefault(r => r.Type == "userId")?.Value;
            try
            {
                var redisValue = await _redisService.GetAsync($"email-reset:{userIdClaim}");

                if (string.IsNullOrEmpty(redisValue))
                    return BadRequest("Verification code expired or not found");

                var redisData = JsonConvert.DeserializeObject<dynamic>(redisValue); ;

                string storedCode = redisData!.Code;

                if (!string.Equals(storedCode, dto.verificationCode, StringComparison.OrdinalIgnoreCase))
                    return BadRequest("Invalid verification code");

                var resetEmailToken = await _authService.getEmailResetToken(Guid.Parse(userIdClaim!));

                if (string.IsNullOrEmpty(resetEmailToken))
                    return StatusCode(500, "Failed to generate reset password token");

                // Удаляем временный код из Redis
                await _redisService.DeleteAsync($"email-reset: {userIdClaim}");

                Response.Cookies.Append("classified-password-reset-token", resetEmailToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = false,
                    Expires = DateTimeOffset.UtcNow.AddMinutes(5)
                });
             
                return Ok("Reset token issued");
            }
            catch (InvalidOperationException e)
            {
                throw new Exception(e.Message);
            }
        }

        [HttpPost("complete-email-change-via-email")]
        public async Task<IActionResult> CompleteEmailChangeViaEmailViaEmail([FromForm] ChangeUserEmailDto email)
        {
            if (!Request.Cookies.TryGetValue("classified-password-reset-token", out var resetPasswordToken) || string.IsNullOrEmpty(resetPasswordToken))
                return Unauthorized("Refresh token is missing or invalid.");

            var handler = new JwtSecurityTokenHandler();

            var resetPasswordJwt = handler.ReadJwtToken(resetPasswordToken);
            var UserIdClaim = resetPasswordJwt.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
            var tokenTypeClaim = resetPasswordJwt.Claims.FirstOrDefault(c => c.Type == "type")?.Value;

            if (tokenTypeClaim != JwtTokenType.EmailReset.ToString())
                return Forbid();

            try
            {
                await _userService.ChangeEmailAsync(Guid.Parse(UserIdClaim!), email.Email);
                return Ok();
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }

        }

        /// <summary>
        /// /////// Reset password via Email
        /// </summary> 

        [HttpPost("change-user-phone-number")]
        public async Task<IActionResult> ChangeUserPhoneNumber([FromForm] ChangeUserPhoneNumberDto phoneNumber)
        {
            var userIdClaim = User.Claims.FirstOrDefault(r => r.Type == "userId")?.Value;

            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            try
            {
                await _userService.ChangePhoneNumberAsync(userId, phoneNumber.PhoneNumber);
                return Ok();
            }
            catch (Exception e) 
            {
                throw new Exception(e.Message);
            }

        }

        [HttpPost("change-user-password")]
        public async Task<IActionResult> ChangeUserPassword([FromForm] ChangeUserPassordDto dto)
        {
            var userIdClaim = User.Claims.FirstOrDefault(r => r.Type == "userId")?.Value;

            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized();
            try
            {
                await _userService.ChangeUserPasswordWithOldPasswordVerification(userId, dto.OldPassword, dto.NewPassword);
                return Ok();
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        /// <summary>
        /// / 
        /// </summary>
        ///             Private methods
        /// <param ></param>
        /// <returns></returns>
        /// <exception></exception>

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

        private async Task<IActionResult> DeletePhotoAsync(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return BadRequest("URL cannot be null or empty.");
            }

            try
            {
                await _fileStorageService.DeleteFileAsync(url);

                return NoContent();
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }


    }

    
}