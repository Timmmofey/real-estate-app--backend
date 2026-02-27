using AuthService.Domain.Abstactions;
using AuthService.Infrastructure.Jwt;
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

        public async Task<VerifiedUserDto?> VerifyUserCredentialsAsync(string phoneOrEmail, string password)
        {
            _http.SetServerJwt(_microserviceJwtProvider, InternalServices.AuthService, InternalServices.UserService);

            var queryParams = new Dictionary<string, string?>
            {
                { "phoneOrEmail", phoneOrEmail },
                { "password", password }
            };

            var url = QueryHelpers.AddQueryString("api/users/verify-user-credentials", queryParams);

            var response = await _http.PostAsync(url, null);
            if (!response.IsSuccessStatusCode) return null;

            return await response.Content.ReadFromJsonAsync<VerifiedUserDto>();
        }

        public async Task<VerifiedUserDto?> GetVerifiedUserDtoByIdAsync(string userId)
        {
            _http.SetServerJwt(_microserviceJwtProvider, InternalServices.AuthService, InternalServices.UserService);

            var queryParams = new Dictionary<string, string?>
            {
                { "userId", userId },
            };

            var url = QueryHelpers.AddQueryString("api/users/get-verified-user-dto-by-id", queryParams);

            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            return await response.Content.ReadFromJsonAsync<VerifiedUserDto>();
        }


        public async Task<UserOAuthAccountDto?> GetUserOAuthAccountByProviderAndProviderUserIdAsync(OAuthProvider provider,string providerUserId)
        {
            _http.SetServerJwt(_microserviceJwtProvider, InternalServices.AuthService, InternalServices.UserService);

            var queryParams = new Dictionary<string, string?>
            {
                { "providerName", provider.ToString() },
                { "providerUserId", providerUserId },
            };

            var url = QueryHelpers.AddQueryString(
                "api/users/get-user-o-auth-account-by-provider-and-provider-user-id-async",
                queryParams);

            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content
                .ReadFromJsonAsync<UserOAuthAccountDto>();
        }

        
        public async Task<string?> GetUserIdByEmailAsync(string email)
        {
            _http.SetServerJwt(_microserviceJwtProvider, InternalServices.AuthService, InternalServices.UserService);

            var queryParams = new Dictionary<string, string?>
            {
                { "email", email },
            };

            var url = QueryHelpers.AddQueryString("api/users/get-user-id-by-email-async", queryParams);

            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadAsStringAsync();
        }

        public async Task ConnectOauthAccountToExistingUserAsync(OAuthProvider provider, string providerId, Guid userId)
        {
            _http.SetServerJwt(_microserviceJwtProvider, InternalServices.AuthService, InternalServices.UserService);

            var request = new
            {
                Provider = provider,
                ProviderId = providerId,
                UserId = userId
            };

            var response = await _http.PostAsJsonAsync("api/users/connect-oauth-account-to-existing-user", request);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                throw new Exception($"Ошибка при подключении OAuth аккаунта: {errorMessage}");
            }
        }
    }
}
