using Classified.Shared.Infrastructure.MicroserviceJwt;
using System.Net.Http.Headers;

namespace Classified.Shared.Extensions
{
    public static class HttpClientExtensions
    {
        public static void SetServerJwt(this HttpClient http, IMicroserviceJwtProvider jwtProvider, string service, string audience, int expiresMinutes = 1)
        {
            var token = jwtProvider.GenerateToken(service, audience, expiresMinutes);
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }
}
