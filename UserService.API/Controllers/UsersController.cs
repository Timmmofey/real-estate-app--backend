using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Classified.Shared.Constants;
using UserService.Application.DTOs;
using UserService.Application.Abstactions;
using Classified.Shared.Functions;
using Classified.Shared.Filters;

namespace UserService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("add-person-user")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreatePersonUser([FromForm] CreatePersonUserDto dto)
        {
            try
            {
                var userId = await _userService.CreatePersonUserAsync(dto);
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

        [HttpPost("verify-user-credentials")]
        public async Task<IActionResult> VerifyUserCredentials(string phoneOrEmail, string password)
        {
            try
            {
                var verifiedUserDto = await _userService.VerifyUsersCredentials(phoneOrEmail, password);

                return Ok(verifiedUserDto);
            }
            catch (InvalidOperationException e)
            {
                return NotFound(e.Message);
            }

        }

        [AuthorizeToken(JwtTokenType.Access)]
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

        [AuthorizeToken(JwtTokenType.Access)]
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

        [AuthorizeToken(JwtTokenType.Access)]
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

        [AuthorizeToken(JwtTokenType.Restore)]
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
            catch (InvalidOperationException e) { 
                throw new Exception(e.Message);
            }
        }

        [AuthorizeToken(JwtTokenType.Restore)]
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

        [AuthorizeToken(JwtTokenType.Access)]
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

        [AuthorizeToken(JwtTokenType.Access)]
        [HttpGet("set-two-factor-authentication")]
        public async Task<IActionResult> SetTwoFactorAuthentication(string userId, bool flag)
        {
            try
            {
                await _userService.SetTwoFactorAuthentication(userId, flag);
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


        [AuthorizeToken(JwtTokenType.PasswordReset)]
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
        public async Task<UserRole?> GetUserById(string userId)
        {
            var user =  await _userService.GetUserById(Guid.Parse(userId));

            return user?.Role;
        }

        /// <summary>
        /// /////// Email Change Via Email
        /// </summary>      

        [AuthorizeToken(JwtTokenType.Access)]
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

        [AuthorizeToken(JwtTokenType.Access)]
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

        [AuthorizeToken(JwtTokenType.RequestNewEmailCofirmation)]
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

        [AuthorizeToken(JwtTokenType.Access)]
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
            catch (InvalidOperationException e)
            {
                throw new Exception(e.Message);
            }
        }

        [AuthorizeToken(JwtTokenType.EmailReset)]
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

    }

    
}