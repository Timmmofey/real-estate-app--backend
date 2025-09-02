using Microsoft.AspNetCore.WebUtilities;
using System.Net.Http.Json;

namespace UserService.Infrastructure.AuthService
{
    public class AuthServiceClient: IAuthServiceClient
    {
        private readonly HttpClient _http;

        public AuthServiceClient(HttpClient http)
        {
            _http = http;
        }

        public async Task<string?> getResetPasswordToken(Guid userId)
        {
            var response = await _http.GetAsync($"api/auth/get-password-reset-token?userId={userId}");

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string?> getEmailResetToken(Guid userId, string newEmail)
        {
            var response = await _http.GetAsync($"api/auth/get-email-reset-token?userId={userId}&newEmail={newEmail}");

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string?> getRequestNewEmailCofirmationToken(Guid userId)
        {
            var response = await _http.GetAsync($"api/auth/get-request-new-email-cofirmation-token?userId={userId}");

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadAsStringAsync();
        }
    }
}
