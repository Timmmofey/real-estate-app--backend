using AuthService.API.Resources;
using AuthService.Application.Exceptions;
using AuthService.Domain.Abstactions;
using AuthService.Domain.DTOs;
using Classified.Shared.Constants;
using Classified.Shared.Filters;
using Classified.Shared.Functions;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Classified.Shared.DTOs;

namespace AuthService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IJwtProvider _jwtProvider;
        private readonly IStringLocalizer<Messages> _localizer;
        private readonly IConfiguration _config;
        private readonly IUserServiceClient _userServiceClient;


        public AuthController(IAuthService authService, IJwtProvider jwtProvider, IStringLocalizer<Messages> localizer, IConfiguration config, IUserServiceClient userServiceClient)
        {
            _authService = authService;
            _jwtProvider = jwtProvider;
            _localizer = localizer;
            _config = config;
            _userServiceClient = userServiceClient;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequestDto dto)
        {
            try
            {
                var deviceId = GetOrCreateDeviceId();

                var (tokens, restoreToken, twoFactorAuthenticatinToken) = await _authService.LoginAsync(dto.PhoneOrEmail, dto.Password, deviceId);

                if (restoreToken != null)
                {
                    CookieHepler.SetCookie(Response, CookieNames.Restore, restoreToken, minutes: 5);

                    return Ok(new { restore = true });
                }

                if (twoFactorAuthenticatinToken != null)
                {
                    CookieHepler.SetCookie(Response, CookieNames.TwoFactorAuthentication, twoFactorAuthenticatinToken, minutes: 5);

                    return Ok(new { isTwoFactorAuth = true });
                }

                CookieHepler.SetCookie(Response, CookieNames.Auth, tokens!.AccessToken, minutes: 10);
                CookieHepler.SetCookie(Response, CookieNames.Refresh, tokens.RefreshToken, days: 150);

                return Ok();
            }
            catch (BlockedUserAccountException)
            {
                return Conflict(new { Message = _localizer["BlockedUserAccountException"] });
            }
            catch (InvalidСredentialsException)
            {
                return Conflict(new { Message = _localizer["InvalidСredentialsException"] });
            }
            catch (Exception ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
        }

        [AuthorizeToken(JwtTokenType.TwoFactorAuthentication)]
        [HttpPost("login-via-two-factor-auth")]
        public async Task<IActionResult> LoginViaTwoFactorAuth([FromBody]string code)
        {
            if (!Request.Cookies.TryGetValue(CookieNames.TwoFactorAuthentication, out var twoFactorAuthenticationToken) || string.IsNullOrEmpty(code))
                return Unauthorized("2FA token is missing or invalid.");

            if (!Request.Cookies.TryGetValue(CookieNames.Device , out var deviceIdToken) || string.IsNullOrEmpty(code))
                return Unauthorized("Device token is missing or invalid.");

            var handler = new JwtSecurityTokenHandler();

            var twoFactorAuthJwt = handler.ReadJwtToken(twoFactorAuthenticationToken);
            var UserIdClaim = twoFactorAuthJwt.Claims.FirstOrDefault(c => c.Type == "userId")!.Value;

            var deviceJwt = handler.ReadJwtToken(deviceIdToken);
            var deviceIdClaim = deviceJwt.Claims.FirstOrDefault(c => c.Type == "deviceId")!.Value;

            try
            {
                var tokens = await _authService.LoginViaTWoFactorAuthentication(UserIdClaim, deviceIdClaim, code);

                CookieHepler.SetCookie(Response, CookieNames.Auth, tokens!.AccessToken, minutes: 10);
                CookieHepler.SetCookie(Response, CookieNames.Refresh, tokens.RefreshToken, days: 150);

                return Ok();
            }
            catch (Exception ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }

        }

        [HttpGet("google")]
        public IActionResult GoogleLogin()
        {
            return Challenge(
                new AuthenticationProperties
                {
                    RedirectUri = "/api/auth/google-callback"
                },
                GoogleDefaults.AuthenticationScheme
            );
        }

        [AllowAnonymous]
        [HttpGet("google-callback")]
        public async Task<IActionResult> GoogleCallback()
        {
            // 1. Получаем результат внешней аутентификации
            var authResult = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
            if (!authResult.Succeeded || authResult.Principal == null)
                return BadRequest("External authentication failed");

            // 2. Считываем нужные claims
            var claims = authResult.Principal.Claims.ToDictionary(c => c.Type, c => c.Value);
            string providerUserId = claims.ContainsKey("sub") ? claims["sub"] : (claims.GetValueOrDefault(ClaimTypes.NameIdentifier) ?? "");
            string? email = claims.GetValueOrDefault(ClaimTypes.Email) ?? claims.GetValueOrDefault("email");
            string? picture = claims.GetValueOrDefault("picture");
            string? firstName = claims.GetValueOrDefault(ClaimTypes.GivenName);
            string? lastName = claims.GetValueOrDefault(ClaimTypes.Surname);



            var frontendBaseUrl = _config["Frontend:BaseUrl"] ?? "http://localhost:3000";
            var provider = OAuthProvider.Google;

            // 3. Если уже есть запись UserOAuthAccount (provider+providerUserId) => логиним
            var existingOAuthAccount = await _userServiceClient.GetUserOAuthAccountByProviderAndProviderUserIdAsync(provider, providerUserId);
            if (existingOAuthAccount != null)
            {
                // имеем userId
                var userId = existingOAuthAccount.UserId;

                // создаём/получаем deviceId
                var deviceId = GetOrCreateDeviceId();

                // вызываем метод аутентификации через AuthService для OAuth-пользователя
                // Возвращаем (tokens, restoreToken, twoFactorToken) по аналогии с LoginAsync
                //(TokenResponseDto? tokens, string? restoreToken, string? twoFactorAuthToken) = await _authService.LoginWithOAuthAsync(userId, deviceId);

                var (tokens, restoreToken, twoFactorAuthenticatinToken) = await _authService.LoginWithOAuthAsync(userId, deviceId);


                if (restoreToken != null)
                {
                    CookieHepler.SetCookie(Response, CookieNames.Restore, restoreToken, minutes: 5);
                    return Redirect($"{frontendBaseUrl}/restoreaccount");
                }

                if (twoFactorAuthenticatinToken != null)
                {
                    CookieHepler.SetCookie(Response, CookieNames.TwoFactorAuthentication, twoFactorAuthenticatinToken, minutes: 5);
                    return Redirect($"{frontendBaseUrl}/twofactorauth");
                }

                CookieHepler.SetCookie(Response, CookieNames.Auth, tokens!.AccessToken, minutes: 10);
                CookieHepler.SetCookie(Response, CookieNames.Refresh, tokens.RefreshToken, days: 150);


                return Redirect(frontendBaseUrl);
            }

            // 4. Нет привязки по provider+id — проверяем, есть ли пользователь с таким email
            if (!string.IsNullOrEmpty(email))
            {
                var existingUserId = await _authService.GetUserIdByEmailAsync(email);
                if (!string.IsNullOrWhiteSpace(existingUserId))
                {
                    // email занят — просим пользователя войти и привязать внешний провайдер вручную
                    // редиректим на фронт, который покажет инструкцию "Войдите в аккаунт, затем привяжите Google"
                    var redirect = $"{frontendBaseUrl}/oauth/link?email={Uri.EscapeDataString(email)}&provider={Uri.EscapeDataString(provider.ToString())}";
                    return Redirect(redirect);
                }
            }

            // 5. Нет пользователя — генерируем registration token и отправляем на страницу дорегистрации
            // Токен нужен чтобы фронтенд корректно предзаполнил форму регистрации и затем вызвал CreateFromOAuth
            var registrationToken = _jwtProvider.GenerateRegistrationToken(email ?? "", provider.ToString(), providerUserId, picture);

            // можно передать токен через query или через cookie
            // передаём в query + ставим куку на 10 минут для безопасности (или поменяйте на вашу логику)
            CookieHepler.SetCookie(Response, CookieNames.OAuthRegistration, registrationToken, minutes: 10);
            var registerRedirect = $"{frontendBaseUrl}/signup" +$"?oauth={Uri.EscapeDataString(email ?? "")}";
            return Redirect(registerRedirect);
        }


        [AuthorizeToken(JwtTokenType.Refresh)]
        [HttpPost("Refresh")]
        public async Task<IActionResult> Refresh()
        {
            // 1. Чтение device-token
            Guid deviceId;

            var errorResult = TryGetDeviceIdFromCookie(out deviceId);
            if (errorResult != null)
                return errorResult;

            // 2. Чтение refresh-токена
            if (!Request.Cookies.TryGetValue(CookieNames.Refresh, out var refreshTokenString) ||
                string.IsNullOrEmpty(refreshTokenString))
            {
                return Unauthorized("Refresh token is missing or invalid.");
            }

            Guid refreshToken;
            var handler = new JwtSecurityTokenHandler();

            var refreshJwt = handler.ReadJwtToken(refreshTokenString);
            var refreshTokenClaim = refreshJwt.Claims.FirstOrDefault(c => c.Type == "token")?.Value;
            
            if (!Guid.TryParse(refreshTokenClaim, out refreshToken))
                return Unauthorized("Refresh token claim is invalid.");

            try
            {
                // 3. Получение новых токенов
                var tokens = await _authService.RefreshAsync(refreshToken, deviceId);

                CookieHepler.SetCookie(Response, CookieNames.Auth, tokens.AccessToken, minutes: 10);
                CookieHepler.SetCookie(Response, CookieNames.Refresh, tokens.RefreshToken, days: 150);
                CookieHepler.SetCookie(Response, CookieNames.Device, tokens.DeviceToken, days: 150);

                return Ok(tokens);
            }
            catch
            {
                return Unauthorized("Refresh token is malformed.");
            }
        }

        [AuthorizeToken(JwtTokenType.Access)]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            Guid deviceId;

            var errorResult = TryGetDeviceIdFromCookie(out deviceId);
            if (errorResult != null)
                return errorResult;

            await _authService.LogoutAync(deviceId);

            CookieHepler.RemoveRefreshAuthDeviceTokens(Response);

            return NoContent();
        }


        [AuthorizeToken(JwtTokenType.Access)]
        [HttpPost("logout-all")]
        public async Task<IActionResult> LogoutAll()
        {
            var userId = User.Claims.FirstOrDefault(r => r.Type == "userId")?.Value;

            if (userId == null)
            {
                return Unauthorized();
            }

            CookieHepler.RemoveRefreshAuthDeviceTokens(Response);

            await _authService.LogoutAllAsync(Guid.Parse(userId));
            return NoContent();
        }

        [AuthorizeToken(JwtTokenType.Access)]
        [HttpPost("terminate-session")]
        public async Task<IActionResult> TerminateSessionAsync(Guid sessionId)
        {
            if (sessionId == Guid.Empty)
                return BadRequest("Session id is required.");

            var userIdClaim = User.Claims.FirstOrDefault(r => r.Type == "userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var success = await _authService.TerminateSession(userId, sessionId);

            if (!success)
                return NotFound("Session not found or already terminated.");

            return NoContent();
        }

        [AuthorizeToken(JwtTokenType.Access)]
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

            if (sessions == null || !sessions.Any())
            {
                CookieHepler.RemoveRefreshAuthDeviceTokens(Response);
                return NoContent();
            }

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

            if (!Request.Cookies.TryGetValue(CookieNames.Device, out var deviceToken) ||
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

        private Guid GetOrCreateDeviceId()
        {
            var handler = new JwtSecurityTokenHandler();

            Guid deviceId = Guid.NewGuid();

            if (!Request.Cookies.TryGetValue(CookieNames.Device, out var deviceToken) || string.IsNullOrEmpty(deviceToken))
            {
                var deviceJwt = _jwtProvider.GenerateDeviceToken(deviceId);

                CookieHepler.SetCookie(Response, CookieNames.Device, deviceJwt, days: 150);
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

                CookieHepler.SetCookie(Response, CookieNames.Device, deviceJwt, days: 150);
            }

            return deviceId;
        }

    }
}
