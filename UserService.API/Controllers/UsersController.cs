using Classified.Shared.Constants;
using Classified.Shared.DTOs;
using Classified.Shared.Extensions;
using Classified.Shared.Extensions.Auth;
using Classified.Shared.Functions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using UserService.API.Resources;
using UserService.Application.Abstactions;
using UserService.Application.DTOs;
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
        private readonly ITokenValidationService _tokenValidator;


        public UsersController(IUserService userService, IStringLocalizer<Messages> localizer, IUserOAuthAccountSevice userOAuthAccountSevice, ITokenValidationService tokenValidator)
        {
            _userService = userService;
            _localizer = localizer;
            _userOAuthAccountService = userOAuthAccountSevice;
            _tokenValidator = tokenValidator;
        }

        [HttpPost("add-person-user")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreatePersonUser([FromForm] CreatePersonUserDto dto, CancellationToken ct)
        {
            var userId = await _userService.CreatePersonUserAsync(dto, ct);

            return Created($"/users/{userId}", new { Message = _localizer["UserCreated"], UserId = userId });
        }

        [HttpPost("add-company-user")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateCompanyUser([FromForm] CreateCompanyUserDto dto, CancellationToken ct)
        {
            var userId = await _userService.CreateCompanyUserAsync(dto, ct);

            return Created($"/users/{userId}", new { Message = "User has been created successfully", UserId = userId });
        }

        [Authorize(Policy = nameof(JwtTokenType.OAuthRegistration))]
        [HttpPost("complete-oauth-registration")]
        public async Task<IActionResult> CompleteOAuthRegistration([FromForm] CompleteOAuthRegistrationDto dto, CancellationToken ct)
        {
            if (!Request.Cookies.TryGetValue(CookieNames.OAuthRegistration, out var token))
                return Unauthorized();

            var principal = _tokenValidator.ValidateAndGetPrincipal(token, JwtTokenType.OAuthRegistration);

            var email = principal.FindFirst(ClaimTypes.Email)?.Value
                ?? throw new SecurityTokenException("Missing email claim");

            var providerUserId = principal.FindFirst("providerUserId")?.Value
                ?? throw new SecurityTokenException("Missing providerUserId");

            var provider = principal.FindFirst("provider")?.Value
                ?? throw new SecurityTokenException("Missing provider");

            if (!Enum.TryParse<OAuthProvider>(provider, ignoreCase: true, out var oauthProviderName))
                throw new ArgumentException($"uknown OAuth provider: {provider}");

            if (!Enum.TryParse<UserRole>(dto.UserRole, ignoreCase: true, out var userRole))
                throw new ArgumentException($"uknown UserRole provider: {dto.UserRole}");

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

                await _userService.CreatePersonUserFromOAuthAsync(personDto, ct);
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

                await _userService.CreateCompanyUserFromOAuthAsync(companyDto, ct);
            }

            CookieHepler.DeleteCookie(Response, CookieNames.OAuthState);
            return Ok();
        }

        [AccessAuthorize(Roles = "Person")]
        [HttpPatch("edit-person-profile-main-info")]
        public async Task<IActionResult> PatchPersonProfile([FromForm] EditPersonUserRequest updatedProfile, CancellationToken ct)
        {
            var userId = ClaimsPrincipalExtensions.GetUserId(Request);

            await _userService.PatchPersonProfileAsync(userId, updatedProfile, ct);

            return NoContent();
        }

        [AccessAuthorize(Roles = "Company")]
        [HttpPatch("edit-company-profile-main-info")]
        public async Task<IActionResult> PatchCompanyProfile([FromForm] EditCompanyUserRequest updatedProfile, CancellationToken ct)
        {
            var userId = ClaimsPrincipalExtensions.GetUserId(Request);

            await _userService.PatchCompanyProfileAsync(userId, updatedProfile, ct);

            return NoContent();
        }

        [Authorize(Roles = "Person, Company")]
        [HttpDelete("delete-account")]
        public async Task<IActionResult> DeleteAccount(CancellationToken ct)
        {
            var userId = ClaimsPrincipalExtensions.GetUserId(Request);

            await _userService.SoftDeleteAccount(userId, ct);

            CookieHepler.RemoveRefreshAuthDeviceTokens(Response);

            return Ok();
        }

        [Authorize(Policy = nameof(JwtTokenType.Restore))]
        [HttpPost("restore-deleted-account")]
        public async Task<IActionResult> RestoreDeletedAccount(CancellationToken ct)
        {
            var userId = ClaimsPrincipalExtensions.GetUserId(Request, CookieNames.Restore);

            await _userService.RestoreDeletedAccount(userId, ct);

            CookieHepler.DeleteCookie(Response, CookieNames.Restore);

            return NoContent();
        }

        [Authorize(Policy = nameof(JwtTokenType.Restore))]
        [HttpDelete("permanantly-delete-account")]
        public async Task<IActionResult> PermanantlyDeleteAccount(CancellationToken ct)
        {
            var userId = ClaimsPrincipalExtensions.GetUserId(Request, CookieNames.Restore);

            await _userService.PermanantlyDeleteAccount(userId, ct);

            CookieHepler.DeleteCookie(Response, CookieNames.Restore);

            return NoContent();
        }

        [AccessAuthorize]
        [HttpGet("get-current-user-info")]
        public async Task<IActionResult> GetPersonalInfo(CancellationToken ct)
        {
            var userId = ClaimsPrincipalExtensions.GetUserId(Request);
            var userRole = ClaimsPrincipalExtensions.GetUserRole(Request);

            var profile = await _userService.GetUserProfileInfo(userId, userRole, ct);

            return Ok(profile);
        }

        /// <summary>
        /// /////// Toggle 2FA
        /// </summary> 

        [AccessAuthorize]
        [HttpPost("request-toggle-two-factor-authentication-code")]
        public async Task<IActionResult> RequestToggleTwoFactorAuthenticationCode(CancellationToken ct)
        {
            var userId = ClaimsPrincipalExtensions.GetUserId(Request);

            await _userService.RequestToggleTwoFactorAuthenticationCode(userId, ct);

            return Ok();
        }

        [AccessAuthorize]
        [HttpPost("toggle-two-factor-authentication")]
        public async Task<IActionResult> ToggleTwoFactorAuthentication(VerificationCodeDto dto, CancellationToken ct)
        {
            var userId = ClaimsPrincipalExtensions.GetUserId(Request);

            await _userService.ToggleTwoFactorAuthentication(userId, dto.Code, ct);

            CookieHepler.DeleteCookie(Response, CookieNames.TwoFactorAuthentication);

            return Ok();
        }

        /// <summary>
        /// /////// Reset Password Via Email
        /// </summary> 

        [HttpPost("start-password-reset-via-email")]
        public async Task<IActionResult> StartPasswordResetViaEmail([FromForm] string email, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest("Email is required");

            await _userService.StartPasswordResetViaEmail(email, ct);

            return Ok();
        }

        [HttpPost("get-password-reset-token-via-email")]
        public async Task<IActionResult> GetPasswordResetTokenViaEmail([FromBody] GetPasswordResetTokenDto dto, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.VerificationCode))
                return BadRequest("Email and verification code are required");

            var resetPasswordToken = await _userService.GetPasswordResetTokenViaEmail(dto, ct);

            CookieHepler.SetCookie(Response, CookieNames.PasswordReset, resetPasswordToken, minutes: 5);

            return Ok("Reset token issued");
        }

        [Authorize(Policy = nameof(JwtTokenType.PasswordReset))]
        [HttpPost("complete-password-restoration-via-email")]
        public async Task<IActionResult> CompletePasswordResorationViaEmail([FromForm] string newPassword, CancellationToken ct)
        {
            var userId = ClaimsPrincipalExtensions.GetUserId(Request, CookieNames.PasswordReset);

            await _userService.ChangePasswordAsync(userId, newPassword, ct);

            CookieHepler.DeleteCookie(Response, CookieNames.PasswordReset);

            return Ok();
        }

        [HttpGet("get-user-role-by-id")]
        public async Task<UserRole?> GetUserRoleById(string userId, CancellationToken ct)
        {
            var user = await _userService.GetUserById(Guid.Parse(userId), ct);

            return user?.Role;
        }

        /// <summary>
        /// /////// Email Change Via Email
        /// </summary>      

        [AccessAuthorize]
        [HttpPost("start-email-change-via-email")]
        public async Task<IActionResult> StartEmailChangeViaEmailViaEmail(CancellationToken ct)
        {
            var userIdClaim = User.Claims.FirstOrDefault(r => r.Type == "userId")?.Value;

            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            await _userService.StartEmailChangeViaEmailViaEmail(userId, ct);
            return Ok("Reset code sent");
        }

        [HttpPost("confirm-current-email")]
        public async Task<IActionResult> ConfirmCurrentEmail([FromBody] EmailResetVerificationCodeDto dto, CancellationToken ct)
        {
            var userId = ClaimsPrincipalExtensions.GetUserId(Request);

            var resetEmailToken = await _userService.GetResetEmailToken(userId, dto.verificationCode, ct);

            CookieHepler.SetCookie(Response, CookieNames.RequestNewEmailCofirmation, resetEmailToken, minutes: 5);

            return Ok("Reset token issued");
        }

        [Authorize(Policy = nameof(JwtTokenType.RequestNewEmailCofirmation))]
        [HttpPost("send-new-email-cofirmation-code")]
        public async Task<IActionResult> SendCofirmationCodeToNewEmail([FromBody] EmailDto dto, CancellationToken ct)
        {
            var userId = ClaimsPrincipalExtensions.GetUserId(Request, CookieNames.RequestNewEmailCofirmation);

            await _userService.SendCofirmationCodeToNewEmail(userId, dto.email, ct);
            CookieHepler.DeleteCookie(Response, CookieNames.RequestNewEmailCofirmation);

            return Ok("Reset code sent");
        }

        [AccessAuthorize]
        [HttpPost("confirm-new-email")]
        public async Task<IActionResult> ConfirmNewEmail([FromBody] EmailResetVerificationCodeDto dto, CancellationToken ct)
        {
            var userId = ClaimsPrincipalExtensions.GetUserId(Request);

            var resetEmailToken = await _userService.ConfirmNewEmail(userId, dto.verificationCode, ct);

            CookieHepler.SetCookie(Response, CookieNames.EmailReset, resetEmailToken, minutes: 5);

            return Ok("Reset token issued");
        }

        [Authorize(Policy = nameof(JwtTokenType.EmailReset))]
        [HttpPost("complete-email-change-via-email")]
        public async Task<IActionResult> CompleteEmailChangeViaEmailViaEmail(CancellationToken ct)
        {
            var userId = ClaimsPrincipalExtensions.GetUserId(Request, CookieNames.EmailReset);
            var newEmail = ClaimsPrincipalExtensions.GetEmailFromEmailResetCookie(Request);

            await _userService.ChangeEmailAsync(userId, newEmail, ct);

            CookieHepler.DeleteCookie(Response, CookieNames.EmailReset);

            return Ok();
        }

        /// <summary>
        /// /////// Reset password via Email
        /// </summary> 

        [AccessAuthorize]
        [HttpPost("change-user-phone-number")]
        public async Task<IActionResult> ChangeUserPhoneNumber([FromForm] ChangeUserPhoneNumberDto phoneNumber, CancellationToken ct)
        {
            var userId = ClaimsPrincipalExtensions.GetUserId(Request);

            await _userService.ChangePhoneNumberAsync(userId, phoneNumber.PhoneNumber, ct);

            return Ok();
        }

        [AccessAuthorize]
        [HttpPost("change-user-password")]
        public async Task<IActionResult> ChangeUserPassword([FromForm] ChangeUserPassordDto dto, CancellationToken ct)
        {
            var userId = ClaimsPrincipalExtensions.GetUserId(Request);

            await _userService.ChangeUserPasswordWithOldPasswordVerification(userId, dto.OldPassword, dto.NewPassword, ct);

            return Ok();
        }

        /// <summary>
        /// /////// OAuth
        /// </summary> 
        
        [AccessAuthorize]
        [HttpGet("get-my-o-auth-accounts")]
        public async Task<ActionResult<ICollection<UserOAuthAccountDto>>> GetMyOAuthAccounts(CancellationToken ct)
        {
            var userId = ClaimsPrincipalExtensions.GetUserId(Request);

            var res = await _userOAuthAccountService.GetUsersOAuthAccountsByUserId(userId, ct);

            return Ok(res);
        }

        [AccessAuthorize]
        [HttpPost("unlink-oauth-account-from-me")]
        public async Task<IActionResult> UnlinkOAuthAccountFromMe([FromQuery] OAuthProvider provider, CancellationToken ct)
        {
            var userId = ClaimsPrincipalExtensions.GetUserId(Request);

            await _userOAuthAccountService.UnLinkOAuthAccountAsync(provider, userId, ct);

            return Ok();
        }
    }


}