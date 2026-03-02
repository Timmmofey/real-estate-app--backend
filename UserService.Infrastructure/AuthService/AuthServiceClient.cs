using Classified.Shared.Constants;
using Classified.Shared.Extensions;
using Classified.Shared.Infrastructure.MicroserviceJwt;
using System.Net.Http.Json;

namespace UserService.Infrastructure.AuthService
{
    public class AuthServiceClient: IAuthServiceClient
    {
        private readonly HttpClient _http;
        private readonly IMicroserviceJwtProvider _microserviceJwtProvider;

        public AuthServiceClient(HttpClient http, IMicroserviceJwtProvider microserviceJwtProvider)
        {
            _http = http;
            _microserviceJwtProvider = microserviceJwtProvider;
        }

        public async Task<string?> getResetPasswordToken(Guid userId)
        {
            _http.SetServerJwt(_microserviceJwtProvider, InternalServices.UserService, InternalServices.AuthService);

            var request = new
            {
                UserId = userId
            };

            var response = await _http.PostAsJsonAsync("internal-api/auth/get-password-reset-token", request);

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string?> getEmailResetToken(Guid userId, string newEmail)
        {
            _http.SetServerJwt(_microserviceJwtProvider, InternalServices.UserService, InternalServices.AuthService);

            var request = new
            {
                UserId = userId,
                NewEmail = newEmail
            };

            var response = await _http.PostAsJsonAsync($"internal-api/auth/get-email-reset-token", request);

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string?> getRequestNewEmailCofirmationToken(Guid userId)
        {
            _http.SetServerJwt(_microserviceJwtProvider, InternalServices.UserService, InternalServices.AuthService);

            var request = new
            {
                UserId = userId
            };

            var response = await _http.PostAsJsonAsync($"internal-api/auth/get-request-new-email-cofirmation-token", request);

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadAsStringAsync();
        }
    }
}
