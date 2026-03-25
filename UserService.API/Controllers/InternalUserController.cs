using Classified.Shared.Constants;
using Classified.Shared.DTOs;
using Classified.Shared.Extensions.ServerJwtAuth;
using Classified.Shared.Extensions.ServerJwtAuth.Classified.Shared.Extensions.ServerJwtAuth;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.Abstactions;
using UserService.Application.DTOs;
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
        public async Task<Guid?> GetUserIdByEmailAsync(string email, CancellationToken ct)
        {
            var result = await _userService.GetUserIdByEmailAsync(email, ct);

            return result;
        }

        [AuthorizeServerJwt(InternalServices.AuthService)]
        [AuthorizeServerJwtBySub("verify-user-credentials")]
        [HttpPost("verify-user-credentials")]
        public async Task<IActionResult> VerifyUserCredentials([FromBody] VerifyUserCredentialsRequestDto dto, CancellationToken ct)
        {
            var verifiedUserDto = await _userService.VerifyUsersCredentials(dto.PhoneOrEmail, dto.Password, ct);

            return Ok(verifiedUserDto);
        }

        [AuthorizeServerJwt(InternalServices.AuthService)]
        [HttpGet("get-verified-user-dto-by-id")]
        public async Task<VerifiedUserDto?> GetVerifiedUserDtoById(string userId, CancellationToken ct)
        {
            var user = await _userService.GetVerifiedUserDtoById(Guid.Parse(userId), ct);

            return user;
        }

        [AuthorizeServerJwt(InternalServices.AuthService)]
        [HttpPost("connect-oauth-account-to-existing-user")]
        public async Task<IActionResult> ConnectOauthAccountToExistingUser([FromBody] ConnectOAuthAccountRequest request, CancellationToken ct)
        {
            await _userOAuthAccountService.ConnectOauthAccountToExistingUser(request.Provider, request.ProviderId, request.UserId, ct);

            return Ok();
        }

        [AuthorizeServerJwt(InternalServices.AuthService)]
        [HttpGet("get-user-o-auth-account-by-provider-and-provider-user-id-async")]
        public async Task<ActionResult<UserOAuthAccountDto>> GetUserOAuthAccountByProviderAndProviderUserIdAsync(
            string providerName,
            string providerUserId,
            CancellationToken ct)
        {
            if (!Enum.TryParse<OAuthProvider>(providerName, true, out var provider))
            {
                return BadRequest("Unknown OAuth provider");
            }

            var result = await _userOAuthAccountService.GetUserOAuthAccountByProviderAndProviderUserId(provider, providerUserId, ct);

            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }
    }
}