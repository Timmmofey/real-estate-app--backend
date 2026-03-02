using Classified.Shared.Constants;
using Classified.Shared.DTOs;
using Classified.Shared.Extensions.ServerJwtAuth;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.Abstactions;
using UserService.Application.DTOs;
using UserService.Application.Exeptions;
using UserService.Application.Services;

namespace UserService.API.Controllers
{
    [Route("internal-api/users")]
    [ApiController]
    public class InternalUserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IUserOAuthAccountSevice _userOAuthAccountService;

        public InternalUserController(IUserService userService, IUserOAuthAccountSevice userOAuthAccountSevice)
        {
            _userService = userService;
            _userOAuthAccountService = userOAuthAccountSevice;
        }

        [AuthorizeServerJwt(InternalServices.AuthService)]
        [HttpGet("get-user-id-by-email-async")]
        public async Task<Guid?> GetUserIdByEmailAsync(string email)
        {
            var result = await _userService.GetUserIdByEmailAsync(email);

            return result;
        }

        [AuthorizeServerJwt(InternalServices.AuthService)]
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

        [AuthorizeServerJwt(InternalServices.AuthService)]
        [HttpGet("get-verified-user-dto-by-id")]
        public async Task<VerifiedUserDto?> GetVerifiedUserDtoById(string userId)
        {
            var user = await _userService.GetVerifiedUserDtoById(Guid.Parse(userId));

            return user;
        }

        [AuthorizeServerJwt(InternalServices.AuthService)]
        [HttpPost("connect-oauth-account-to-existing-user")]
        public async Task<IActionResult> ConnectOauthAccountToExistingUser([FromBody] ConnectOAuthAccountRequest request)
        {
            try
            {
                await _userOAuthAccountService.ConnectOauthAccountToExistingUser(request.Provider, request.ProviderId, request.UserId);
                return Ok();
            }
            catch (OAuthAccountAlreadyLinkedException e)
            {
                return Conflict(e.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [AuthorizeServerJwt(InternalServices.AuthService)]
        [HttpGet("get-user-o-auth-account-by-provider-and-provider-user-id-async")]
        public async Task<ActionResult<UserOAuthAccountDto>> GetUserOAuthAccountByProviderAndProviderUserIdAsync(
            string providerName,
            string providerUserId)
        {
            if (!Enum.TryParse<OAuthProvider>(providerName, true, out var provider))
            {
                return BadRequest("Unknown OAuth provider");
            }

            var result = await _userOAuthAccountService.GetUserOAuthAccountByProviderAndProviderUserId(provider, providerUserId);

            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }
    }
}