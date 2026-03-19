using Classified.Shared.Infrastructure.MicroserviceJwt;
using System.Net.Http.Headers;

namespace Classified.Shared.Extensions
{
    public static class HttpClientExtensions
    {
        public static void SetServerJwt(this HttpClient http, IMicroserviceJwtProvider jwtProvider, string audience, string? subject = null, int expiresMinutes = 1)
        {
            var token = jwtProvider.GenerateToken(audience, subject, expiresMinutes);
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }
}
