using Classified.Shared.Constants;
using Classified.Shared.Extensions.ErrorHandler.Errors;
using Classified.Shared.Infrastructure.MicroserviceJwt;
using Classified.Shared.Libs;
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

        private readonly string _serviceName = InternalServices.AuthService;


        public async Task<string?> GetResetPasswordTokenAsync(Guid userId, CancellationToken ct)
        {
            _http.SetServerJwt(_microserviceJwtProvider, _serviceName);

            var request = new
            {
                UserId = userId,
            };

            var response = await _http.PostAsJsonAsync($"internal-api/auth/get-password-reset-token", request, ct);

            if (!response.IsSuccessStatusCode)
                throw new Exception("eror occured while getting pwd reset token");

            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string?> GetEmailResetTokenAsync(Guid userId, string newEmail, CancellationToken ct)
        {
            _http.SetServerJwt(_microserviceJwtProvider, _serviceName);

            var request = new
            {
                UserId = userId,
                NewEmail = newEmail
            };

            var response = await _http.PostAsJsonAsync($"internal-api/auth/get-email-reset-token", request, ct);

            if (!response.IsSuccessStatusCode)
                throw new Exception("eror occured while getting email reset token");

            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string?> GetRequestNewEmailCofirmationTokenAsync(Guid userId, CancellationToken ct)
        {
            _http.SetServerJwt(_microserviceJwtProvider, _serviceName);

            var request = new
            {
                UserId = userId
            };

            var response = await _http.PostAsJsonAsync($"internal-api/auth/get-request-new-email-cofirmation-token", request, ct);

            if (!response.IsSuccessStatusCode)
                throw new Exception("eror occured while getting new email confirmation token");

            return await response.Content.ReadAsStringAsync();
        }
    }
}
