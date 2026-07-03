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

        //[HttpPost("add-person-user")]
        [HttpPost("person")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreatePersonUser([FromForm] CreatePersonUserRequestDto dto, CancellationToken ct)
        {
            var userId = await _userService.CreatePersonUserAsync(dto, ct);

            return Created($"/users/{userId}", new { Message = _localizer["UserCreated"], UserId = userId });
        }

        //[HttpPost("add-company-user")]
        [HttpPost("company")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateCompanyUser([FromForm] CreateCompanyUserRequestDto dto, CancellationToken ct)
        {
            var userId = await _userService.CreateCompanyUserAsync(dto, ct);

            return Created($"/users/{userId}", new { Message = "User has been created successfully", UserId = userId });
        }

        //[HttpPost("complete-oauth-registration")]
        [Authorize(Policy = nameof(JwtTokenType.OAuthRegistration))]
        [HttpPost("oauth/complete")]
        public async Task<IActionResult> CompleteOAuthRegistration([FromForm] CompleteOAuthRegistrationRequestDto dto, CancellationToken ct)
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
                var companyDto = new CreateCompanyUserOAuthRequestDto
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

        //[HttpPatch("edit-person-profile-main-info")]
        [AccessAuthorize(Roles = "Person")]
        [HttpPatch("me/profile/person")]
        public async Task<IActionResult> PatchPersonProfile([FromForm] EditPersonUserRequest updatedProfile, CancellationToken ct)
        {
            var userId = ClaimsPrincipalExtensions.GetUserId(Request);

            await _userService.PatchPersonProfileAsync(userId, updatedProfile, ct);

            return NoContent();
        }

        //[HttpPatch("edit-company-profile-main-info")]
        [AccessAuthorize(Roles = "Company")]
        [HttpPatch("me/profile/сompany")]
        public async Task<IActionResult> PatchCompanyProfile([FromForm] EditCompanyUserRequestDto updatedProfile, CancellationToken ct)
        {
            var userId = ClaimsPrincipalExtensions.GetUserId(Request);

            await _userService.PatchCompanyProfileAsync(userId, updatedProfile, ct);

            return NoContent();
        }

        //[HttpDelete("delete-account")]
        [Authorize(Roles = "Person, Company")]
        [HttpPatch("me")]
        public async Task<IActionResult> DeleteAccount(CancellationToken ct)
        {
            var userId = ClaimsPrincipalExtensions.GetUserId(Request);

            await _userService.SoftDeleteAccount(userId, ct);

            CookieHepler.RemoveRefreshAuthDeviceTokens(Response);

            return Ok();
        }

        //[HttpPost("restore-deleted-account")] 
        [Authorize(Policy = nameof(JwtTokenType.Restore))]
        [HttpPost("me/restore")]
        public async Task<IActionResult> RestoreDeletedAccount(CancellationToken ct)
        {
            var userId = ClaimsPrincipalExtensions.GetUserId(Request, CookieNames.Restore);

            await _userService.RestoreDeletedAccount(userId, ct);

            CookieHepler.DeleteCookie(Response, CookieNames.Restore);

            return NoContent();
        }

        //[HttpDelete("permanantly-delete-account")]
        [Authorize(Policy = nameof(JwtTokenType.Restore))]
        [HttpDelete("me/permanent")]
        public async Task<IActionResult> PermanantlyDeleteAccount(CancellationToken ct)
        {
            var userId = ClaimsPrincipalExtensions.GetUserId(Request, CookieNames.Restore);

            await _userService.PermanantlyDeleteAccount(userId, ct);

            CookieHepler.DeleteCookie(Response, CookieNames.Restore);

            return NoContent();
        }

        //[HttpGet("get-current-user-info")]
        [AccessAuthorize]
        [HttpGet("me")]
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

        //[HttpPost("request-toggle-two-factor-authentication-code")]
        [AccessAuthorize]
        [HttpPost("me/2fa/toggle-request")]
        public async Task<IActionResult> RequestToggleTwoFactorAuthenticationCode(CancellationToken ct)
        {
            var userId = ClaimsPrincipalExtensions.GetUserId(Request);

            await _userService.RequestToggleTwoFactorAuthenticationCode(userId, ct);

            return Ok();
        }

        //[HttpPost("toggle-two-factor-authentication")]
        [AccessAuthorize]
        [HttpPut("me/2fa-toggle")]
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

        //[HttpPost("start-password-reset-via-email")]
        [HttpPost("password-reset/request")]
        public async Task<IActionResult> StartPasswordResetViaEmail([FromForm] string email, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest("Email is required");

            await _userService.StartPasswordResetViaEmail(email, ct);

            return Ok();
        }

        //[HttpPost("get-password-reset-token-via-email")]
        [HttpPost("password-reset/verify")]
        public async Task<IActionResult> GetPasswordResetTokenViaEmail([FromBody] GetPasswordResetTokenRequestDto dto, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.VerificationCode))
                return BadRequest("Email and verification code are required");

            var resetPasswordToken = await _userService.GetPasswordResetTokenViaEmail(dto, ct);

            CookieHepler.SetCookie(Response, CookieNames.PasswordReset, resetPasswordToken, minutes: 5);

            return Ok("Reset token issued");
        }

        //[HttpPost("complete-password-restoration-via-email")]
        [Authorize(Policy = nameof(JwtTokenType.PasswordReset))]
        [HttpPut("password-reset/complete")]
        public async Task<IActionResult> CompletePasswordResorationViaEmail([FromForm] string newPassword, CancellationToken ct)
        {
            var userId = ClaimsPrincipalExtensions.GetUserId(Request, CookieNames.PasswordReset);

            await _userService.ChangePasswordAsync(userId, newPassword, ct);

            CookieHepler.DeleteCookie(Response, CookieNames.PasswordReset);

            return Ok();
        }

        //[HttpGet("get-user-role-by-id")]
        [HttpGet("{userId}/role")]
        public async Task<UserRole?> GetUserRoleById(string userId, CancellationToken ct)
        {
            var user = await _userService.GetUserById(Guid.Parse(userId), ct);

            return user?.Role;
        }

        /// <summary>
        /// /////// Email Change Via Email
        /// </summary>      

        //[HttpPost("start-email-change-via-email")]
        [AccessAuthorize]
        [HttpPost("me/email-change/request")]
        public async Task<IActionResult> StartEmailChangeViaEmailViaEmail(CancellationToken ct)
        {
            var userIdClaim = User.Claims.FirstOrDefault(r => r.Type == "userId")?.Value;

            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            await _userService.StartEmailChangeViaEmailViaEmail(userId, ct);
            return Ok("Reset code sent");
        }

        //[HttpPost("confirm-current-email")]
        [HttpPost("me/email-change/confirm-current")]
        public async Task<IActionResult> ConfirmCurrentEmail([FromBody] VerificationCodeDto dto, CancellationToken ct)
        {
            var userId = ClaimsPrincipalExtensions.GetUserId(Request);

            var resetEmailToken = await _userService.GetResetEmailToken(userId, dto.Code, ct);

            CookieHepler.SetCookie(Response, CookieNames.RequestNewEmailCofirmation, resetEmailToken, minutes: 5);

            return Ok("Reset token issued");
        }

        //[HttpPost("send-new-email-cofirmation-code")]
        [Authorize(Policy = nameof(JwtTokenType.RequestNewEmailCofirmation))]
        [HttpPost("me/email-change/send-new-code")]
        public async Task<IActionResult> SendCofirmationCodeToNewEmail([FromBody] EmailRequestDto dto, CancellationToken ct)
        {
            var userId = ClaimsPrincipalExtensions.GetUserId(Request, CookieNames.RequestNewEmailCofirmation);

            await _userService.SendCofirmationCodeToNewEmail(userId, dto.Email, ct);
            CookieHepler.DeleteCookie(Response, CookieNames.RequestNewEmailCofirmation);

            return Ok("Reset code sent");
        }

        //[HttpPost("confirm-new-email")]
        [AccessAuthorize]
        [HttpPost("me/email-change/confirm-new")]
        public async Task<IActionResult> ConfirmNewEmail([FromBody] VerificationCodeDto dto, CancellationToken ct)
        {
            var userId = ClaimsPrincipalExtensions.GetUserId(Request);

            var resetEmailToken = await _userService.ConfirmNewEmail(userId, dto.Code, ct);

            CookieHepler.SetCookie(Response, CookieNames.EmailReset, resetEmailToken, minutes: 5);

            return Ok("Reset token issued");
        }

        //[HttpPost("complete-email-change-via-email")]
        [Authorize(Policy = nameof(JwtTokenType.EmailReset))]
        [HttpPut("me/email-change/complete")]
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

        //[HttpPost("change-user-phone-number")]
        [AccessAuthorize]
        [HttpPut("me/phone")]
        public async Task<IActionResult> ChangeUserPhoneNumber([FromForm] ChangeUserPhoneNumberRequestDto phoneNumber, CancellationToken ct)
        {
            var userId = ClaimsPrincipalExtensions.GetUserId(Request);

            await _userService.ChangePhoneNumberAsync(userId, phoneNumber.PhoneNumber, ct);

            return Ok();
        }

        //[HttpPost("change-user-password")]
        [AccessAuthorize]
        [HttpPut("me/password")]
        public async Task<IActionResult> ChangeUserPassword([FromForm] ChangeUserPassordRequestDto dto, CancellationToken ct)
        {
            var userId = ClaimsPrincipalExtensions.GetUserId(Request);

            await _userService.ChangeUserPasswordWithOldPasswordVerification(userId, dto.OldPassword, dto.NewPassword, ct);

            return Ok();
        }

        /// <summary>
        /// /////// OAuth
        /// </summary> 
        
        //[HttpGet("get-my-o-auth-accounts")]
        [AccessAuthorize]
        [HttpGet("me/oauth-accounts")]
        public async Task<ActionResult<ICollection<UserOAuthAccountDto>>> GetMyOAuthAccounts(CancellationToken ct)
        {
            var userId = ClaimsPrincipalExtensions.GetUserId(Request);

            var res = await _userOAuthAccountService.GetUsersOAuthAccountsByUserId(userId, ct);

            return Ok(res);
        }

        //[HttpPost("unlink-oauth-account-from-me")]
        [AccessAuthorize]
        [HttpDelete("me/oauth-accounts/{provider}")]
        public async Task<IActionResult> UnlinkOAuthAccountFromMe([FromRoute] OAuthProvider provider, CancellationToken ct)
        {
            var userId = ClaimsPrincipalExtensions.GetUserId(Request);

            await _userOAuthAccountService.UnLinkOAuthAccountAsync(provider, userId, ct);

            return Ok();
        }
    }


}