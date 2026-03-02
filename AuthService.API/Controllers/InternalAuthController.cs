using AuthService.Domain.Abstactions;
using Classified.Shared.Constants;
using Classified.Shared.Extensions.ServerJwtAuth;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace AuthService.API.Controllers
{
    [Route("internal-api/auth")]
    [ApiController]
    public class InternalAuthController : ControllerBase
    {
        private readonly IJwtProvider _jwtProvider;


        public InternalAuthController(IJwtProvider jwtProvider)
        {
            _jwtProvider = jwtProvider;
        }

        [AuthorizeServerJwt(InternalServices.UserService)]
        [HttpPost("get-password-reset-token")]
        public IActionResult getResetPasswordResetToken([FromBody] string userId)
        {
            var resetPasswordJwt = _jwtProvider.GenerateResetPasswordResetToken(Guid.Parse(userId));
            return Ok(resetPasswordJwt);
        }

        [AuthorizeServerJwt(InternalServices.UserService)]
        [HttpPost("get-email-reset-token")]
        public IActionResult getResetEmailResetToken([FromBody] JsonElement body)
        {
            var userId = body.GetProperty("userId").GetString();
            var newEmail = body.GetProperty("newEmail").GetString();

            var resetEmailJwt = _jwtProvider.GenerateResetEmailResetToken(Guid.Parse(userId), newEmail);
            return Ok(resetEmailJwt);
        }

        [AuthorizeServerJwt(InternalServices.UserService)]
        [HttpPost("get-request-new-email-cofirmation-token")]
        public IActionResult getRequestNewEmailCofirmationToken([FromBody] JsonElement body)
        {
            var userId = body.GetProperty("userId").GetString();
            var resetPasswordJwt = _jwtProvider.GenerateRequestNewEmailCofirmationToken(Guid.Parse(userId));
            return Ok(resetPasswordJwt);
        }
    }
}
