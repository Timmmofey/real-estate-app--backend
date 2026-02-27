using Classified.Shared.Constants;
using Classified.Shared.Extensions;
using Classified.Shared.Infrastructure.MicroserviceJwt;

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

            var response = await _http.GetAsync($"api/auth/get-password-reset-token?userId={userId}");

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string?> getEmailResetToken(Guid userId, string newEmail)
        {
            _http.SetServerJwt(_microserviceJwtProvider, InternalServices.UserService, InternalServices.AuthService);

            var response = await _http.GetAsync($"api/auth/get-email-reset-token?userId={userId}&newEmail={newEmail}");

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string?> getRequestNewEmailCofirmationToken(Guid userId)
        {
            _http.SetServerJwt(_microserviceJwtProvider, InternalServices.UserService, InternalServices.AuthService);

            var response = await _http.GetAsync($"api/auth/get-request-new-email-cofirmation-token?userId={userId}");

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadAsStringAsync();
        }
    }
}
