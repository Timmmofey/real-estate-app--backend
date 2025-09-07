using AuthService.Domain.Abstactions;
using AuthService.Domain.DTOs;
using AuthService.Domain.Models;
using Classified.Shared.Constants;
using DeviceDetectorNET;
using Microsoft.AspNetCore.Http;
using UAParser;

namespace AuthService.Application.Services
{
    public class AuthService: IAuthService
    {
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IUserServiceClient _userServiceClient;
        private readonly IJwtProvider _jwtProvider;
        private readonly IHttpContextAccessor _http;

        public AuthService(IRefreshTokenRepository refreshTokenRepository, IUserServiceClient userServiceClient, IJwtProvider jwtProvider, IHttpContextAccessor http)
        {
            _refreshTokenRepository = refreshTokenRepository;
            _userServiceClient = userServiceClient;
            _jwtProvider = jwtProvider;
            _http = http;
        }

        public async Task<(TokenResponseDto?, string?)> LoginAsync(string phoneOrEmail, string password, Guid deviceId)
        {
            var user = await _userServiceClient.VerifyUserCredentialsAsync(phoneOrEmail, password);

            if (user == null)
                throw new UnauthorizedAccessException("Invalid credentials");

            if (user.IsDeleted == true)
            {
                var restoreToken = _jwtProvider.GenerateRestoreToken(user.Id);
                return (null, restoreToken);
            }

            var sessionId = Guid.NewGuid();
            var accessToken = _jwtProvider.GenerateAccessToken(user.Id, user.Role, sessionId);
            var refreshToken = Guid.NewGuid();
            var deviceToken = _jwtProvider.GenerateDeviceToken(deviceId);

            var (deviceName, deviceType, ipAddress, country, city) = await GetDeviceInfo();

            var (refreshTokenObj, error) = Session.Create(
                sessionId,
                user.Id,
                user.Role,
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

            var clientsRefreshToken = _jwtProvider.GenerateRefreshToken(refreshToken);

            if (refreshTokenObj == null)
                throw new Exception($"Refresh token creation failed: {error}");

            await _refreshTokenRepository.AddOrUpdateRefreshTokenAsync(refreshTokenObj);

            return (new TokenResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = clientsRefreshToken,
                DeviceToken = deviceToken
            }, null);
        }

        public async Task<TokenResponseDto> RefreshAsync(Guid refreshToken, Guid deviceId)
        {
            var session = await _refreshTokenRepository.FindSessionByRefreshTokenAndDeviceId(refreshToken, deviceId);

            if (session == null) throw new UnauthorizedAccessException("Session does not exist");

  
            var accessToken = _jwtProvider.GenerateAccessToken(session.UserId, session.Role, session.Id);
            var newrefreshToken = Guid.NewGuid();
            var deviceToken = _jwtProvider.GenerateDeviceToken(session.DeviceId);

            var (deviceName, deviceType, ipAddress, country, city) = await GetDeviceInfo();


            var (refreshTokenObj, error) = Session.Create(
                session.Id,
                session.UserId,
                session.Role,
                newrefreshToken,
                deviceId,
                deviceName,
                deviceType,
                ipAddress,
                country,
                city,
                null,
                null
            );

            if (refreshTokenObj == null)
                throw new Exception($"Refresh token creation failed: {error}");
            var newRefreshToken = _jwtProvider.GenerateRefreshToken(newrefreshToken);

            await _refreshTokenRepository.AddOrUpdateRefreshTokenAsync(refreshTokenObj);

            return new TokenResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken,
                DeviceToken = deviceToken
            };
        }

        public async Task LogoutAync(Guid deviceId) 
        {
            await _refreshTokenRepository.DeleteRefreshTokenByUserIdAndDeviceIDAsync(deviceId);
        } 

        public async Task LogoutAllAsync(Guid userId)
        {
            await _refreshTokenRepository.DeleteAllRefreskTokensByUserId(userId);
        }

        public async Task<bool> TerminateSession(Guid userId, Guid id)
        {
            return await _refreshTokenRepository.DeleteRefreshTokenByUserIdAndIdAsync(userId, id);
        }

        public async Task<ICollection<SessionDto>> GetUsersSessions(Guid userId, Guid sessionId)
        {
            var refreshTokens = await _refreshTokenRepository.GetUsersSessionsAsync(userId);
            var usersSessions = new List<SessionDto>();

            foreach (var refreshToken in refreshTokens)
            {
                var session = new SessionDto
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

        private async Task<(string DeviceName, DeviceType? DeviceType, string? IpAddress, string? Country, string? City)> GetDeviceInfo()
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


            string? country = null;
            string? city = null;

            
            if (!string.IsNullOrEmpty(ipAddress))
            {

                if (ipAddress == "127.0.0.1" || ipAddress == "::1")
                {
                    ipAddress = "104.196.181.192"; // пусть вместо локал хоста будет, хоть увидим как апи определяет
                }

                try
                {
                    using var http = new HttpClient();
                    var response = await http.GetStringAsync($"http://ip-api.com/json/{ipAddress}");
                    var data = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(response);
                    if (data.TryGetProperty("country", out var countryProp))
                        country = countryProp.GetString();
                    if (data.TryGetProperty("city", out var cityProp))
                        city = cityProp.GetString();
                }
                catch
                {
                }
            }

            return (deviceName, deviceType, ipAddress, country, city);
        }


    }
}