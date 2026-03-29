using AuthService.Domain.Abstactions;
using Classified.Shared.Constants;
using Classified.Shared.Extensions.ServerJwtAuth;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.DTOs;

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
        public IActionResult getResetPasswordResetToken(UserIdRequestDto dto)
        {
            var resetPasswordJwt = _jwtProvider.GenerateResetPasswordResetToken(Guid.Parse(dto.UserId));

            return Ok(resetPasswordJwt);
        }

        [AuthorizeServerJwt(InternalServices.UserService)]
        [HttpPost("get-email-reset-token")]
        public IActionResult getResetEmailResetToken([FromBody] GetResetEmailResetTokenRequestDto dto)
        {
            var resetEmailJwt = _jwtProvider.GenerateResetEmailResetToken(Guid.Parse(dto.userId), dto.newEmail);

            return Ok(resetEmailJwt);
        }

        [AuthorizeServerJwt(InternalServices.UserService)]
        [HttpPost("get-request-new-email-cofirmation-token")]
        public IActionResult getRequestNewEmailCofirmationToken(UserIdRequestDto dto)
        {
            var resetPasswordJwt = _jwtProvider.GenerateRequestNewEmailCofirmationToken(Guid.Parse(dto.UserId));

            return Ok(resetPasswordJwt);
        }
    }
}
