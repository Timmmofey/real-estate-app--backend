using AuthService.Domain.Abstactions;
using AuthService.Domain.DTOs;
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
    }
}
