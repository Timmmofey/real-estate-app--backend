using AuthService.Domain.Abstactions;
using AuthService.Domain.DTOs;
using Classified.Shared.Constants;
using Classified.Shared.Functions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace AuthService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IJwtProvider _jwtProvider;

        public AuthController(IAuthService authService, IJwtProvider jwtProvider)
        {
            _authService = authService;
            _jwtProvider = jwtProvider;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequestDto dto)
        {
            try
            {
                Guid deviceId = Guid.NewGuid();

                var handler = new JwtSecurityTokenHandler();

         
                if (!Request.Cookies.TryGetValue("classified-device-id-token", out var deviceToken) ||
                string.IsNullOrEmpty(deviceToken))
                {
                    var deviceJwt = _jwtProvider.GenerateDeviceToken(deviceId);

                    CookieHepler.SetCookie(Response, "classified-device-id-token", deviceJwt, days: 150);
                }
                else
                {
                    var token = handler.ReadJwtToken(deviceToken);
                    var deviceTokenClaim = token.Claims.FirstOrDefault(c => c.Type == "deviceId")?.Value;
                    

                    if (!Guid.TryParse(deviceTokenClaim, out deviceId))
                    {
                        deviceId = Guid.NewGuid(); 
                    }
                    var deviceJwt = _jwtProvider.GenerateDeviceToken(deviceId);

                    CookieHepler.SetCookie(Response, "classified-device-id-token", deviceJwt, days: 150);
                }


                var (tokens, restoreToken) = await _authService.LoginAsync(dto.PhoneOrEmail, dto.Password, deviceId);

                if (restoreToken != null)
                {
                    CookieHepler.SetCookie(Response, "classified-restore-token", restoreToken, minutes: 10);

                    return Ok(restoreToken);
                }

                CookieHepler.SetCookie(Response, "classified-auth-token", tokens!.AccessToken, minutes: 10);
                CookieHepler.SetCookie(Response, "classified-refresh-token", tokens.RefreshToken, days: 150);

                return Ok(tokens);
            }
            catch (Exception ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("Refresh")]
        public async Task<IActionResult> Refresh()
        {
            // 1. Чтение device-token
            Guid deviceId;

            var errorResult = TryGetDeviceIdFromCookie(out deviceId);
            if (errorResult != null)
                return errorResult;

            // 2. Чтение refresh-токена
            if (!Request.Cookies.TryGetValue("classified-refresh-token", out var refreshTokenString) ||
                string.IsNullOrEmpty(refreshTokenString))
            {
                return Unauthorized("Refresh token is missing or invalid.");
            }

            Guid refreshToken;
            var handler = new JwtSecurityTokenHandler();
            try
            {
                var refreshJwt = handler.ReadJwtToken(refreshTokenString);
                var refreshTokenClaim = refreshJwt.Claims.FirstOrDefault(c => c.Type == "token")?.Value;
                var tokenTypeClaim = refreshJwt.Claims.FirstOrDefault(c => c.Type == "type")?.Value;

                if (tokenTypeClaim != JwtTokenType.Refresh.ToString())
                    return Forbid();
                if (!Guid.TryParse(refreshTokenClaim, out refreshToken))
                    return Unauthorized("Refresh token claim is invalid.");
            }
            catch
            {
                return Unauthorized("Refresh token is malformed.");
            }

            // 3. Получение новых токенов
            var tokens = await _authService.RefreshAsync(refreshToken, deviceId);

            CookieHepler.SetCookie(Response, "classified-auth-token", tokens.AccessToken, minutes: 10);
            CookieHepler.SetCookie(Response, "classified-refresh-token", tokens.RefreshToken, days: 150);
            CookieHepler.SetCookie(Response, "classified-device-id-token", tokens.DeviceToken, days: 150);

            return Ok(tokens);
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            Guid deviceId;

            var errorResult = TryGetDeviceIdFromCookie(out deviceId);
            if (errorResult != null)
                return errorResult;

            await _authService.LogoutAync(deviceId);

            RemoveRefreshAuthDeviceTokens();

            return NoContent();
        }


        [Authorize]
        [HttpPost("logout-all")]
        public async Task<IActionResult> LogoutAll()
        {
            var userId = User.Claims.FirstOrDefault(r => r.Type == "userId")?.Value;
            var tokenTypeClaim = User.Claims.FirstOrDefault(c => c.Type == "type")?.Value;

            //if (tokenTypeClaim != JwtTokenType.Access.ToString())
            //    return Forbid();

            if (userId == null)
            {
                return Unauthorized();
            }

            RemoveRefreshAuthDeviceTokens();

            await _authService.LogoutAllAsync(Guid.Parse(userId));
            return NoContent();
        }

        [Authorize]
        [HttpPost("terminate-session")]
        public async Task<IActionResult> TerminateSessionAsync(Guid sessionId)
        {
            if (sessionId == Guid.Empty)
                return BadRequest("Session id is required.");

            var userIdClaim = User.Claims.FirstOrDefault(r => r.Type == "userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var tokenTypeClaim = User.Claims.FirstOrDefault(c => c.Type == "type")?.Value;
            if (!string.Equals(tokenTypeClaim, JwtTokenType.Access.ToString(), StringComparison.OrdinalIgnoreCase))
                return Forbid();

            var success = await _authService.TerminateSession(userId, sessionId);

            if (!success)
                return NotFound("Session not found or already terminated.");

            return NoContent();
        }

        [Authorize]
        [HttpGet("get-users-sessions")]
        public async Task<ActionResult<ICollection<SessionDto>>> GetUsersSessionAsync()
        {
            var userIdClaim = User.Claims.FirstOrDefault(r => r.Type == "userId")?.Value;
            var sessionIdClaim = User.Claims.FirstOrDefault(r => r.Type == "sessionId")?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }
            if (!Guid.TryParse(sessionIdClaim, out var sessionId))
            {
                return Unauthorized();
            }

            var sessions = await _authService.GetUsersSessions(userId, sessionId);
            return Ok(sessions);
        }

        [EnableCors("AllowUserService")]
        [HttpGet("get-password-reset-token")]
        public IActionResult getResetPasswordResetToken(string userId)
        {
            var resetPasswordJwt = _jwtProvider.GenerateResetPasswordResetToken(Guid.Parse(userId));
            return Ok(resetPasswordJwt);
        }

        [HttpGet("get-email-reset-token")]
        public IActionResult getResetEmailResetToken(string userId, string newEmail)
        {
            var resetPasswordJwt = _jwtProvider.GenerateResetEmailResetToken(Guid.Parse(userId), newEmail);
            return Ok(resetPasswordJwt);
        }

        [HttpGet("get-request-new-email-cofirmation-token")]
        public IActionResult getRequestNewEmailCofirmationToken(string userId)
        {
            var resetPasswordJwt = _jwtProvider.GenerateRequestNewEmailCofirmationToken(Guid.Parse(userId));
            return Ok(resetPasswordJwt);
        }

        /// <summary>
        ///
        /// </summary>

        private IActionResult? TryGetDeviceIdFromCookie(out Guid deviceId)
        {
            deviceId = Guid.Empty;

            if (!Request.Cookies.TryGetValue("classified-device-id-token", out var deviceToken) ||
                string.IsNullOrEmpty(deviceToken))
            {
                return Unauthorized("Device token is missing or invalid.");
            }

            var handler = new JwtSecurityTokenHandler();
            try
            {
                var token = handler.ReadJwtToken(deviceToken);
                var deviceTokenClaim = token.Claims.FirstOrDefault(c => c.Type == "deviceId")?.Value;
                if (!Guid.TryParse(deviceTokenClaim, out deviceId))
                {
                    return Unauthorized("Device ID claim is invalid.");
                }
                return null;
            }
            catch
            {
                return Unauthorized("Device token is malformed.");
            }
        }

        private void RemoveRefreshAuthDeviceTokens() 
        {
            CookieHepler.DeleteCookie(Response, "classified-auth-token" );
            CookieHepler.DeleteCookie(Response, "classified-refresh-token");
            CookieHepler.DeleteCookie(Response, "classified-device-id-token");
        }

    }
}
