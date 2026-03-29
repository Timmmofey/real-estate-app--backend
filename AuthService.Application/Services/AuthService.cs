using AuthService.Application.Exceptions;
using AuthService.Domain.Abstactions;
using AuthService.Domain.Consts;
using AuthService.Domain.DTOs;
using AuthService.Domain.Models;
using AuthService.Infrastructure.IpGeoService;
using Classified.Shared.Constants;
using Classified.Shared.DTOs;
using Classified.Shared.Infrastructure.EmailService;
using Classified.Shared.Infrastructure.RedisService;
using DeviceDetectorNET;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using UAParser;


namespace AuthService.Application.Services
{
    public class AuthService: IAuthService
    {
        private readonly ISessionRepository _refreshTokenRepository;
        private readonly IUserServiceClient _userServiceClient;
        private readonly IJwtProvider _jwtProvider;
        private readonly IHttpContextAccessor _http;
        private readonly IRedisService _redisService;
        private readonly IEmailService _emailService;
        private readonly IIpGeoService _ipGeoService;

        public AuthService(ISessionRepository refreshTokenRepository, IUserServiceClient userServiceClient, IJwtProvider jwtProvider, IHttpContextAccessor http, IRedisService redisService, IEmailService emailService, IIpGeoService ipGeoService)
        {
            _refreshTokenRepository = refreshTokenRepository;
            _userServiceClient = userServiceClient;
            _jwtProvider = jwtProvider;
            _http = http;
            _redisService = redisService;
            _emailService = emailService;
            _ipGeoService = ipGeoService;
        }

        public async Task<(TokenResponseDto?, string?, string?)> LoginAsync(string phoneOrEmail, string password, Guid deviceId, CancellationToken ct)
        {
            VerifiedUserDto? user;
            //try
            //{
            //     user = await _userServiceClient.VerifyUserCredentialsAsync(phoneOrEmail, password);
            //}
            //catch (Exception ex)
            //{
            //    throw new Exception($"{ex}");
            //}
            user = await _userServiceClient.VerifyUserCredentialsAsync(phoneOrEmail, password, ct);


            if (user == null)
                throw new InvalidСredentialsException();

            if (user.IsDeleted == true)
            {
                var restoreToken = _jwtProvider.GenerateRestoreToken(user.Id);
                return (null, restoreToken, null);
            }

            if (user.IsBlocked == true)
                throw new BlockedUserAccountException();

            if (user.IsTwoFactorEnabled == true)
            {
                var code = Guid.NewGuid().ToString().Substring(0, 11).Replace("-", "").ToUpper();


                var twoFactorAuthenticatinToken = _jwtProvider.GenerateTwoFactorAuthToken(user.Id);

                var redisData = new
                {
                    Code = code,
                    UserRole = user.Role
                };

                //try
                //{
                //    await _redisService.SetAsync(
                //        key: $"{RedisKey.TwoFactorAuth}:{user.Id}",
                //        value: System.Text.Json.JsonSerializer.Serialize(redisData),
                //        expiration: TimeSpan.FromMinutes(5)
                //    );

                //    await _emailService.SendEmail(user.Email, "Two factor auth code", code);
                //}
                //catch (Exception ex)
                //{
                //    throw new Exception($"{ex}");
                //}

                await _redisService.SetAsync(
                        key: $"{RedisKey.TwoFactorAuth}:{user.Id}",
                        value: System.Text.Json.JsonSerializer.Serialize(redisData),
                        expiration: TimeSpan.FromMinutes(5)
                    );

                await _emailService.SendEmail(user.Email, "Two factor auth code", code);

                return (null, null, twoFactorAuthenticatinToken);
            }

            var tokens = await GenerateTokensAfterLogin(user.Id, user.Role, deviceId, ct);

            return (tokens, null, null);
        }

        public async Task<(
            TokenResponseDto? tokens,
            string? restoreToken,
            string? twoFactorAuthToken
        )> LoginWithOAuthAsync(Guid userId, Guid deviceId, CancellationToken ct)
        {
            // 1. Получаем пользователя
            var user = await _userServiceClient.GetVerifiedUserDtoByIdAsync(userId.ToString(), ct);

            if (user == null)
                throw new InvalidСredentialsException();

            // 2. Аккаунт soft-deleted → restore flow
            if (user.IsDeleted == true)
            {
                var restoreToken = _jwtProvider.GenerateRestoreToken(user.Id);
                return (null, restoreToken, null);
            }

            // 3. Заблокирован
            if (user.IsBlocked == true)
            {
                throw new BlockedUserAccountException();
            }

            // 4. Two-Factor Authentication
            if (user.IsTwoFactorEnabled == true)
            {
                var code = Guid.NewGuid()
                    .ToString()
                    .Substring(0, 11)
                    .Replace("-", "")
                    .ToUpper();

                var twoFactorAuthenticationToken =
                    _jwtProvider.GenerateTwoFactorAuthToken(user.Id);

                var redisData = new
                {
                    Code = code,
                    UserRole = user.Role
                };

                //try
                //{
                //    await _redisService.SetAsync(
                //        key: $"{RedisKey.TwoFactorAuth}:{user.Id}",
                //        value: System.Text.Json.JsonSerializer.Serialize(redisData),
                //        expiration: TimeSpan.FromMinutes(5)
                //    );

                //    await _emailService.SendEmail(
                //        user.Email,
                //        "Two factor authentication code",
                //        code
                //    );
                //}
                //catch (Exception ex)
                //{
                //    throw new Exception($"Two-factor auth error: {ex.Message}");
                //}
                await _redisService.SetAsync(
                        key: $"{RedisKey.TwoFactorAuth}:{user.Id}",
                        value: System.Text.Json.JsonSerializer.Serialize(redisData),
                        expiration: TimeSpan.FromMinutes(5)
                    );

                await _emailService.SendEmail(
                    user.Email,
                    "Two factor authentication code",
                    code
                );

                return (null, null, twoFactorAuthenticationToken);
            }

            // 5. Обычный успешный логин
            var tokens = await GenerateTokensAfterLogin(user.Id, user.Role, deviceId, ct);

            return (tokens, null, null);
        }

        public async Task<string?> GetUserIdByEmailAsync(string email, CancellationToken ct)
        {
            return await _userServiceClient.GetUserIdByEmailAsync(email, ct);
        }

        public async Task<UserOAuthAccountDto?> GetUserOAuthAccountByProviderAndProviderUserIdAsync(OAuthProvider provider, string providerUserId, CancellationToken ct)
        {
            var existingOAuthAccount = await _userServiceClient.GetUserOAuthAccountByProviderAndProviderUserIdAsync(provider, providerUserId, ct);

            return existingOAuthAccount;
        }

        public async Task LinkOAuthAccountAsync(OAuthProvider provider, string providerId, Guid userId, CancellationToken ct) 
        {
            await _userServiceClient.ConnectOauthAccountToExistingUserAsync(provider, providerId, userId, ct);
        }

        public async Task<TokenResponseDto?> LoginViaTWoFactorAuthentication(string userId, string deviceId, string code, CancellationToken ct)
        {
            var redisValue = await _redisService.GetAsync($"{RedisKey.TwoFactorAuth}:{userId}");

            if (string.IsNullOrEmpty(redisValue))
                throw new Exception("There is no code for this user");

            var redisData = JsonConvert.DeserializeObject<dynamic>(redisValue);

            string storedCode = redisData!.Code;
            int userRoleValue = (int)redisData!.UserRole;
            var parsedRole = (UserRole)userRoleValue;

            if (!string.Equals(storedCode, code, StringComparison.OrdinalIgnoreCase))
                throw new Exception("Invalid verification code");

            var tokens = await GenerateTokensAfterLogin(Guid.Parse(userId), parsedRole, Guid.Parse(deviceId), ct);

            return tokens;
        }

        public async Task<TokenResponseDto> RefreshAndRorateAsync(Guid refreshToken, Guid deviceId, string prevIp, CancellationToken ct)
        {
            var currentIp = _http.HttpContext?.Connection.RemoteIpAddress?.ToString();
            if (currentIp == null)
                throw new UnauthorizedAccessException("Ip does not exist");

            string? deviceName = null;
            DeviceType? deviceType = null;
            string? ipAddress = null;
            string? country = null;
            string? city = null;

            if (prevIp != currentIp)
            {
                (deviceName, deviceType, ipAddress, country, city) = await GetDeviceInfo();
            }

            var newRefreshTokenGuid = Guid.NewGuid();

            var refreshTokenObj = await _refreshTokenRepository.UpdateAndRotateRefreshTokenAsync(refreshToken, deviceId, newRefreshTokenGuid, ct, deviceName, deviceType, ipAddress, country, city);

            if (refreshTokenObj == null)
                throw new Exception($"Refresh token creation failed");

            var newRefreshToken = _jwtProvider.GenerateRefreshToken(newRefreshTokenGuid, currentIp);
            var deviceToken = _jwtProvider.GenerateDeviceToken(refreshTokenObj.DeviceId);
            var accessToken = _jwtProvider.GenerateAccessToken(refreshTokenObj.UserId, refreshTokenObj.Role, refreshTokenObj.Id);

            return new TokenResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken,
                DeviceToken = deviceToken
            };
        }

        // <summary>
        // РЕФРЕШ С ПОСТОЯННОЙ СМЕНОЙ IP ДЛЯ ТЕСТОВ НА ЛОКАЛКЕ
        // </summary>

        //public async Task<TokenResponseDto> RefreshAndRorateAsync(Guid refreshToken, Guid deviceId, string prevIp)
        //{
        //    string? deviceName;
        //    DeviceType? deviceType;
        //    string? ipAddress;
        //    string? countryName;
        //    string? cityName;


        //    if (prevIp == null)
        //    {
        //        ipAddress = "104.196.180.192";
        //    }
        //    else if (prevIp == "104.196.180.192")
        //    {
        //        ipAddress = "23.246.192.1";
        //    }
        //    else if (prevIp == "23.246.192.1")
        //    {
        //        ipAddress = "104.196.180.192";
        //    }
        //    else
        //    {
        //        ipAddress = "104.196.180.192";
        //    }

        //    (_, countryName, _, cityName, _, _) = await _ipGeoService.LookupAsync(ipAddress);


        //    var newRefreshTokenGuid = Guid.NewGuid();

        //    var refreshTokenObj =
        //        await _refreshTokenRepository.UpdateAndRotateRefreshTokenAsync(
        //            refreshToken,
        //            deviceId,
        //            newRefreshTokenGuid,
        //            null,
        //            null,
        //            ipAddress,
        //            countryName,
        //            cityName);

        //    if (refreshTokenObj == null)
        //        throw new Exception("Refresh token creation failed");

        //    var newRefreshToken =
        //        _jwtProvider.GenerateRefreshToken(newRefreshTokenGuid, ipAddress);

        //    var deviceToken =
        //        _jwtProvider.GenerateDeviceToken(refreshTokenObj.DeviceId);

        //    var accessToken =
        //        _jwtProvider.GenerateAccessToken(
        //            refreshTokenObj.UserId,
        //            refreshTokenObj.Role,
        //            refreshTokenObj.Id);

        //    return new TokenResponseDto
        //    {
        //        AccessToken = accessToken,
        //        RefreshToken = newRefreshToken,
        //        DeviceToken = deviceToken
        //    };
        //}

        public async Task LogoutAync(Guid deviceId, CancellationToken ct) 
        {
            await _refreshTokenRepository.DeleteRefreshTokenByUserIdAndDeviceIDAsync(deviceId, ct);
        } 

        public async Task LogoutAllAsync(Guid userId, CancellationToken ct)
        {
            await _refreshTokenRepository.DeleteAllRefreskTokensByUserId(userId, ct);
        }

        public async Task<bool> TerminateSession(Guid userId, Guid id, CancellationToken ct)
        {
            return await _refreshTokenRepository.DeleteRefreshTokenByUserIdAndIdAsync(userId, id, ct);
        }

        public async Task<ICollection<SessionResponseDto>> GetUsersSessions(Guid userId, Guid sessionId, CancellationToken ct)
        {
            var refreshTokens = await _refreshTokenRepository.GetUsersSessionsAsync(userId, ct);
            var usersSessions = new List<SessionResponseDto>();

            foreach (var refreshToken in refreshTokens)
            {
                var session = new SessionResponseDto
                {
                    DeviceName = refreshToken.DeviceName,
                    DeviceType = refreshToken.DeviceType,
                    IpAddress = refreshToken.IpAddress,
                    SessionId = refreshToken.Id,
                    Country = refreshToken.Country,
                    Settlement = refreshToken.Settlemnet,
                    IsCurrentSession = sessionId == refreshToken.Id,
                    LastActivity = refreshToken.CreatedAt
                };

                if (session != null)
                {
                    usersSessions.Add(session);
                }
            }

            return usersSessions;
        }

        ///////////////////////////////////
        ///
        //////////////////////////////////

        private async Task<(string DeviceName, DeviceType? DeviceType, string? IpAddress, string? Country, string? City)> GetDeviceInfo(string? prevIp = null)
        {
            var context = _http.HttpContext!;
            var uaString = context.Request.Headers["User-Agent"].ToString();
            var client = Parser.GetDefault().Parse(uaString);

            var deviceName = $"{client.UA.Family} {client.UA.Major} on {client.OS.Family} {client.OS.Major}";

            var ipAddress = context.Connection.RemoteIpAddress?.ToString();

            var dd = new DeviceDetector(uaString);
            dd.Parse();

            DeviceType? deviceType = null;

            if (dd.IsBot())
                deviceType = DeviceType.Bot;
            else if (dd.IsMobile())
                deviceType = DeviceType.Mobile;
            else if (dd.IsTablet())
                deviceType = DeviceType.Tablet;
            else if (dd.IsDesktop())
                deviceType = DeviceType.Desktop;
            else if (dd.IsTv())
                deviceType = DeviceType.SmartTv;

            string? countryIso = null;
            string? countryName = null;
            string? region = null;
            string? cityName = null;
         
            if (!string.IsNullOrEmpty(ipAddress))
            {
                bool testIpCahge = false;

                if (testIpCahge)
                {
                    if (prevIp == null)
                    {
                        ipAddress = "104.196.180.192"; // первый заход
                    }
                    else if (prevIp == "104.196.180.192")
                    {
                        ipAddress = "23.246.192.1";
                    }
                    else if (prevIp == "23.246.192.1")
                    {
                        ipAddress = "104.196.180.192";
                    }
                    else
                    {
                        ipAddress = "104.196.180.192";
                    }
                } else
                {
                    if (ipAddress == "127.0.0.1" || ipAddress == "::1")
                    {
                        ipAddress = "104.196.181.192"; // пусть вместо локал хоста будет, хоть увидим как апи определяет
                    }
                }

                (countryIso, countryName, region, cityName, _, _) = await _ipGeoService.LookupAsync(ipAddress);

            }

            return (deviceName, deviceType, ipAddress, countryName, cityName);
        }

        private async Task<TokenResponseDto> GenerateTokensAfterLogin(Guid userId, UserRole role, Guid deviceId, CancellationToken ct)
        {
            var currentIp = _http.HttpContext?.Connection.RemoteIpAddress?.ToString();
            if (currentIp == null)
                throw new UnauthorizedAccessException("Ip does not exist");

            var sessionId = Guid.NewGuid();
            var accessToken = _jwtProvider.GenerateAccessToken(userId, role, sessionId);
            var refreshToken = Guid.NewGuid();
            var deviceToken = _jwtProvider.GenerateDeviceToken(deviceId);

            var (deviceName, deviceType, ipAddress, country, city) = await GetDeviceInfo();

            var (refreshTokenObj, error) = Session.Create(
                sessionId,
                userId,
                role,
                refreshToken,
                deviceId,
                deviceName,
                deviceType,
                ipAddress,
                country,
                city,
                null,
                null
            );

            var clientsRefreshToken = _jwtProvider.GenerateRefreshToken(refreshToken, currentIp);

            if (refreshTokenObj == null)
                throw new Exception($"Refresh token creation failed: {error}");

            await _refreshTokenRepository.AddOrUpdateRefreshTokenAsync(refreshTokenObj, ct);

            return (new TokenResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = clientsRefreshToken,
                DeviceToken = deviceToken
            });
        }
    }
}