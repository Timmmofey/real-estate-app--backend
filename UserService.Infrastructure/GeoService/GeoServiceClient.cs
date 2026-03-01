using Classified.Shared.Constants;
using Classified.Shared.Extensions;
using Classified.Shared.Infrastructure.MicroserviceJwt;

namespace UserService.Infrastructure.GeoService
{
    public class GeoServiceClient : IGeoServiceClient
    {
        private readonly HttpClient _http;
        private readonly IMicroserviceJwtProvider _microserviceJwtProvider;

        public GeoServiceClient(HttpClient http, IMicroserviceJwtProvider microserviceJwtProvider)
        {
            _http = http;
            _microserviceJwtProvider = microserviceJwtProvider;
        }

        public async Task<bool> ValidateSettlement(string countryCode, string regionCode, string settlement)
        {
            _http.SetServerJwt(_microserviceJwtProvider, InternalServices.UserService, InternalServices.GeoService);

            var response = await _http.GetAsync($"api/Geo/verifysettlement?countryCode={countryCode}&regionCode={regionCode}&settlement={settlement}");

            if (!response.IsSuccessStatusCode)
                return false;

            return true;
        }
    }
}
