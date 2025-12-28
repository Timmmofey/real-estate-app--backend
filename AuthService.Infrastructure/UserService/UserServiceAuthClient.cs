using AuthService.Domain.Abstactions;
using Classified.Shared.Constants;
using Classified.Shared.DTOs;
using Microsoft.AspNetCore.WebUtilities;
using System.Net.Http.Json;

namespace AuthService.Infrastructure.UserService
{
    public class UserServiceClient: IUserServiceClient
    {
        private readonly HttpClient _http;

        public UserServiceClient(HttpClient http)
        {
            _http = http;
        }

        public async Task<VerifiedUserDto?> VerifyUserCredentialsAsync(string phoneOrEmail, string password)
        {
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
    }
}
