using Classified.Shared.Constants;
using Classified.Shared.DTOs;
using Classified.Shared.Extensions.Auth;
using Classified.Shared.Extensions.ServerJwtAuth;
using Classified.Shared.Functions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using UserService.API.Resources;
using UserService.Application.Abstactions;
using UserService.Application.DTOs;
using UserService.Application.Exeptions;
using UserService.Application.Services;

namespace UserService.API.Controllers
{
    [Route("api/users")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IUserOAuthAccountSevice _userOAuthAccountService;
        private readonly IStringLocalizer<Messages> _localizer;


        public UsersController(IUserService userService, IStringLocalizer<Messages> localizer, IUserOAuthAccountSevice userOAuthAccountSevice)
        {
            _userService = userService;
            _localizer = localizer;
            _userOAuthAccountService = userOAuthAccountSevice;
        }

        [HttpPost("add-person-user")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreatePersonUser([FromForm] CreatePersonUserDto dto)
        {
            try
            {
                var userId = await _userService.CreatePersonUserAsync(dto);
                return Created($"/users/{userId}", new { Message = _localizer["UserCreated"], UserId = userId });
            }
            catch (UserAlreadyExistsException)
            {
                return Conflict(new { Message = _localizer["UserAlreadyExists"] });
            }
            catch (RecentlyDeletedUserExceptionOnCreating)
            {
                return Conflict(new { Message = _localizer["RecentlyDeletedUser"] });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("add-company-user")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateCompanyUser([FromForm] CreateCompanyUserDto dto)
        {
            try
            {
                var userId = await _userService.CreateCompanyUserAsync(dto);
                return Created($"/users/{userId}", new { Message = "User has been created successfully", UserId = userId });
            }
            catch (InvalidOperationException e)
            {
                return NotFound(e.Message);
            }
        }

        [ValidateToken(JwtTokenType.OAuthRegistration)]
        [HttpPost("complete-oauth-registration")]
        public async Task<IActionResult> CompleteOAuthRegistration([FromForm] CompleteOAuthRegistrationDto dto)
        {
            if (!Request.Cookies.TryGetValue(CookieNames.OAuthRegistration, out var tokenHeader))
            {
                return Unauthorized("Missing device token.");
            }


            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(tokenHeader.ToString());


            var email = jwt.Claims.First(c => c.Type == "email").Value;
            var providerUserId = jwt.Claims.First(c => c.Type == "providerUserId").Value;
            var provider = jwt.Claims.First(c => c.Type == "provider").Value;

            if (!Enum.TryParse<OAuthProvider>(provider, ignoreCase: true, out var oauthProviderName))
            {
                throw new ArgumentException($"uknown OAuth provider: {provider}");
            }

            if (!Enum.TryParse<UserRole>(dto.UserRole, ignoreCase: true, out var userRole))
            {
                throw new ArgumentException($"uknown UserRole provider: {dto.UserRole}");
            }

            try
            {
                if (userRole == UserRole.Person)
                {
                    var personDto = new CreatePersonUserOAuthDto
                    {
                        Email = email,
                        PhoneNumber = dto.PhoneNumber,
                        ProviderUserId = providerUserId,
                        Provider = oauthProviderName,
                        MainPhoto = dto.MainPhoto,
                        FirstName = dto.FirstName!,
                        LastName = dto.LastName!,
                        Country = dto.Country,
                        Region = dto.Region,
                        Settlement = dto.Settlement,
                        ZipCode = dto.ZipCode,
                        Password = dto.Password
                    };

                    await _userService.CreatePersonUserFromOAuthAsync(personDto);
                }
                else
                {
                    var companyDto = new CreateCompanyUserOAuthDto
                    {
                        Email = email,
                        PhoneNumber = dto.PhoneNumber,
                        ProviderUserId = providerUserId,
                        Provider = oauthProviderName,
                        MainPhoto = dto?.MainPhoto,
                        Name = dto!.Name!,
                        RegistrationAdress = dto.RegistrationAdress!,
                        СompanyRegistrationNumber = dto.СompanyRegistrationNumber!,
                        Country = dto.Country!,
                        Region = dto.Region!,
                        Settlement = dto.Settlement!,
                        ZipCode = dto.ZipCode!,
                        Password = dto.Password
                    };

                    await _userService.CreateCompanyUserFromOAuthAsync(companyDto);
                }
            }
            catch (UserAlreadyExistsException)
            {
                return Conflict(new { Message = _localizer["UserAlreadyExists"] });
            }
            catch (RecentlyDeletedUserExceptionOnCreating)
            {
                return Conflict(new { Message = _localizer["RecentlyDeletedUser"] });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }

            CookieHepler.DeleteCookie(Response, CookieNames.OAuthState);
            return Ok();
        }

        

        [Authorize(Roles = "Person")]
        [HttpPatch("edit-person-profile-main-info")]
        public async Task<IActionResult> PatchPersonProfile([FromForm] EditPersonUserRequest updatedProfile)
        {
            var userIdClaim = User.Claims.FirstOrDefault(r => r.Type == "userId")?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            try
            {
                await _userService.PatchPersonProfileAsync(userId, updatedProfile);
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

            try
            {
                await _userService.PatchCompanyProfileAsync(userId, updatedProfile);
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
                CookieHepler.RemoveRefreshAuthDeviceTokens(Response);
                return Ok();
            }
            catch (InvalidOperationException e) {
                throw new Exception(e.Message);
            }
        }

        [ValidateToken(JwtTokenType.Restore)]
        [HttpPost("restore-deleted-account")]
        public async Task<IActionResult> RestoreDeletedAccount()
        {
            if (!Request.Cookies.TryGetValue(CookieNames.Restore, out var tokenHeader))
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
                await _userService.RestoreDeletedAccount(userId);
                CookieHepler.DeleteCookie(Response, CookieNames.Restore);
                return NoContent();
            }
            catch (InvalidOperationException e) 
            {
                throw new Exception(e.Message);
            }
        }

        [ValidateToken(JwtTokenType.Restore)]
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
                CookieHepler.DeleteCookie(Response, CookieNames.Restore);
                return NoContent();
            }
            catch (InvalidOperationException e)
            {
                throw new Exception(e.Message);
            }
        }

        [Authorize]
        [HttpGet("get-current-user-info")]
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


        [Authorize]
        [HttpPost("request-toggle-two-factor-authentication-code")]
        public async Task<IActionResult> RequestToggleTwoFactorAuthenticationCode()
        {
            var userIdClaim = User.Claims.FirstOrDefault(r => r.Type == "userId")?.Value;

            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            try
            {
                await _userService.RequestToggleTwoFactorAuthenticationCode(userId);
                return Ok();
            }
            catch (InvalidOperationException e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("toggle-two-factor-authentication")]
        public async Task<IActionResult> ToggleTwoFactorAuthentication(VerificationCodeDto dto)
        {
            var userIdClaim = User.Claims.FirstOrDefault(r => r.Type == "userId")?.Value;

            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            try
            {
                await _userService.ToggleTwoFactorAuthentication(userId, dto.Code);
                return Ok();
            }
            catch (InvalidOperationException e)
            {
                return BadRequest(e.Message);
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
                await _userService.StartPasswordResetViaEmail(email);
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("get-password-reset-token-via-email")]
        public async Task<IActionResult> GetPasswordResetTokenViaEmail([FromBody] GetPasswordResetTokenDto dto)
        {

            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.VerificationCode))
                return BadRequest("Email and verification code are required");

            try
            {
                var resetPasswordToken = await _userService.GetPasswordResetTokenViaEmail(dto);

                CookieHepler.SetCookie(Response, CookieNames.PasswordReset, resetPasswordToken, minutes: 5);

                return Ok("Reset token issued");
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }


        [ValidateToken(JwtTokenType.PasswordReset)]
        [HttpPost("complete-password-restoration-via-email")]
        public async Task<IActionResult> CompletePasswordResorationViaEmail([FromForm] string newPassword)
        {
            if (!Request.Cookies.TryGetValue(CookieNames.PasswordReset, out var resetPasswordToken) || string.IsNullOrEmpty(resetPasswordToken))
                return Unauthorized("Refresh token is missing or invalid.");

            var handler = new JwtSecurityTokenHandler();

            var resetPasswordJwt = handler.ReadJwtToken(resetPasswordToken);
            var UserIdClaim = resetPasswordJwt.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;

            try
            {
                await _userService.ChangePasswordAsync(Guid.Parse(UserIdClaim!), newPassword);
                CookieHepler.DeleteCookie(Response, CookieNames.PasswordReset);
                return Ok();
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }

        }

        [HttpGet("get-user-role-by-id")]
        public async Task<UserRole?> GetUserRoleById(string userId)
        {
            var user = await _userService.GetUserById(Guid.Parse(userId));

            return user?.Role;
        }

      

        /// <summary>
        /// /////// Email Change Via Email
        /// </summary>      

        [HttpPost("start-email-change-via-email")]
        public async Task<IActionResult> startEmailChangeViaEmailViaEmail()
        {
            var userIdClaim = User.Claims.FirstOrDefault(r => r.Type == "userId")?.Value;

            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            try
            {
                await _userService.startEmailChangeViaEmailViaEmail(userId);
                return Ok("Reset code sent");
            }
            catch (InvalidOperationException e)
            {
                throw new Exception(e.Message);
            }
        }

        [HttpPost("confirm-current-email")]
        public async Task<IActionResult> confirmCurrentEmail([FromBody] EmailResetVerificationCodeDto dto)
        {
            var userIdClaim = User.Claims.FirstOrDefault(r => r.Type == "userId")?.Value;

            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            try
            {
                var resetEmailToken = await _userService.getResetEmailToken(userId, dto.verificationCode);

                CookieHepler.SetCookie(Response, CookieNames.RequestNewEmailCofirmation, resetEmailToken, minutes: 5);

                return Ok("Reset token issued");
            }
            catch (InvalidOperationException e)
            {
                throw new Exception(e.Message);
            }
        }

        [ValidateToken(JwtTokenType.RequestNewEmailCofirmation)]
        [HttpPost("send-new-email-cofirmation-code")]
        public async Task<IActionResult> sendCofirmationCodeToNewEmail([FromBody] EmailDto dto)
        {
            if (!Request.Cookies.TryGetValue(CookieNames.RequestNewEmailCofirmation, out var resetPasswordToken) || string.IsNullOrEmpty(resetPasswordToken))
                return Unauthorized("Refresh token is missing or invalid.");

            var handler = new JwtSecurityTokenHandler();

            var resetPasswordJwt = handler.ReadJwtToken(resetPasswordToken);

            var userIdClaim = resetPasswordJwt.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;

            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            try
            {
                await _userService.sendCofirmationCodeToNewEmail(userId, dto.email);
                CookieHepler.DeleteCookie(Response, CookieNames.RequestNewEmailCofirmation);

                return Ok("Reset code sent");
            }
            catch (InvalidOperationException e)
            {
                throw new Exception(e.Message);
            }
        }

        [ValidateToken(JwtTokenType.Access)]
        [HttpPost("confirm-new-email")]
        public async Task<IActionResult> confirmNewEmail([FromBody] EmailResetVerificationCodeDto dto)
        {
            var userIdClaim = User.Claims.FirstOrDefault(r => r.Type == "userId")?.Value;

            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            try
            {
                var resetEmailToken = await _userService.confirmNewEmail(userId, dto.verificationCode);

                CookieHepler.SetCookie(Response, CookieNames.EmailReset, resetEmailToken, minutes: 5);

                return Ok("Reset token issued");
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        [ValidateToken(JwtTokenType.EmailReset)]
        [HttpPost("complete-email-change-via-email")]
        public async Task<IActionResult> CompleteEmailChangeViaEmailViaEmail()
        {
            if (!Request.Cookies.TryGetValue(CookieNames.EmailReset, out var resetPasswordToken) || string.IsNullOrEmpty(resetPasswordToken))
                return Unauthorized("Refresh token is missing or invalid.");

            var handler = new JwtSecurityTokenHandler();

            var resetPasswordJwt = handler.ReadJwtToken(resetPasswordToken);

            var userIdClaim = resetPasswordJwt.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
            var newEmailClaim = resetPasswordJwt.Claims.FirstOrDefault(c => c.Type == "email")?.Value;

            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            if (string.IsNullOrEmpty(newEmailClaim))
                return NotFound();

            try
            {
                await _userService.ChangeEmailAsync(userId, newEmailClaim);
                CookieHepler.DeleteCookie(Response, CookieNames.EmailReset);
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
        /// /////// OAuth
        /// </summary> 
        /// 
        

        [Authorize]
        [HttpGet("get-my-o-auth-accounts")]
        public async Task<ActionResult<ICollection<UserOAuthAccountDto>>> getMyOAuthAccounts()
        {
            var userIdClaim = User.Claims.FirstOrDefault(r => r.Type == "userId")?.Value;

            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var res = await _userOAuthAccountService.GetUsersOAuthAccountsByUserId(userId);

            return Ok(res);
        }

      

        [HttpPost("unlink-oauth-account-from-me")]
        public async Task<IActionResult> UnlinkOAuthAccountFromMe([FromQuery] OAuthProvider provider)
        {
            var userIdClaim = User.Claims.FirstOrDefault(r => r.Type == "userId")?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized();
            try
            {
                await _userOAuthAccountService.UnLinkOAuthAccountAsync(provider, userId);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }


}