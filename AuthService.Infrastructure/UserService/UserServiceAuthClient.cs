using AuthService.Domain.Abstactions;
using Classified.Shared.Constants;
using Classified.Shared.DTOs;
using Classified.Shared.Extensions;
using Classified.Shared.Infrastructure.MicroserviceJwt;
using Microsoft.AspNetCore.WebUtilities;
using System.Net.Http.Json;

namespace AuthService.Infrastructure.UserService
{
    public class UserServiceClient: IUserServiceClient
    {
        private readonly HttpClient _http;
        private readonly IMicroserviceJwtProvider _microserviceJwtProvider;

        public UserServiceClient(HttpClient http, IMicroserviceJwtProvider microserviceJwtProvider)
        {
            _http = http;
            _microserviceJwtProvider = microserviceJwtProvider;
        }

        private readonly string _serviceName = InternalServices.UserService;

        public async Task<VerifiedUserDto?> VerifyUserCredentialsAsync(string phoneOrEmail, string password)
        {
            _http.SetServerJwt(_microserviceJwtProvider, _serviceName, "verify-user-credentials");

            var request = new
            {
                PhoneOrEmail = phoneOrEmail,
                Password = password
            };

            var response = await _http.PostAsJsonAsync($"internal-api/users/verify-user-credentials", request);

            if (!response.IsSuccessStatusCode) 
                throw new UnauthorizedAccessException($"error:{response.RequestMessage}");

            return await response.Content.ReadFromJsonAsync<VerifiedUserDto>();
        }

        public async Task<VerifiedUserDto?> GetVerifiedUserDtoByIdAsync(string userId)
        {
            _http.SetServerJwt(_microserviceJwtProvider, _serviceName);

            var queryParams = new Dictionary<string, string?>
            {
                { "userId", userId },
            };

            var url = QueryHelpers.AddQueryString("internal-api/users/get-verified-user-dto-by-id", queryParams);

            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            return await response.Content.ReadFromJsonAsync<VerifiedUserDto>();
        }


        public async Task<UserOAuthAccountDto?> GetUserOAuthAccountByProviderAndProviderUserIdAsync(OAuthProvider provider,string providerUserId)
        {
            _http.SetServerJwt(_microserviceJwtProvider, _serviceName);

            var queryParams = new Dictionary<string, string?>
            {
                { "providerName", provider.ToString() },
                { "providerUserId", providerUserId },
            };

            var url = QueryHelpers.AddQueryString(
                "internal-api/users/get-user-o-auth-account-by-provider-and-provider-user-id-async",
                queryParams);

            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content
                .ReadFromJsonAsync<UserOAuthAccountDto>();
        }

        
        public async Task<string?> GetUserIdByEmailAsync(string email)
        {
            _http.SetServerJwt(_microserviceJwtProvider, _serviceName);

            var queryParams = new Dictionary<string, string?>
            {
                { "email", email },
            };

            var url = QueryHelpers.AddQueryString("internal-api/users/get-user-id-by-email-async", queryParams);

            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadAsStringAsync();
        }

        public async Task ConnectOauthAccountToExistingUserAsync(OAuthProvider provider, string providerId, Guid userId)
        {
            _http.SetServerJwt(_microserviceJwtProvider, _serviceName);

            var request = new
            {
                Provider = provider,
                ProviderId = providerId,
                UserId = userId
            };

            var response = await _http.PostAsJsonAsync("internal-api/users/connect-oauth-account-to-existing-user", request);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                throw new Exception($"Ошибка при подключении OAuth аккаунта: {errorMessage}");
            }
        }
    }
}
