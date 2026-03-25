using Classified.Shared.Constants;
using Classified.Shared.Extensions;
using Classified.Shared.Infrastructure.MicroserviceJwt;
using Classified.Shared.Libs;

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
        private readonly string _serviceName = InternalServices.GeoService;

        public async Task<bool> ValidateSettlement(string countryCode, string regionCode, string settlement)
        {
            _http.SetServerJwt(_microserviceJwtProvider, _serviceName);

            var response = await _http.GetAsync($"internal-api/Geo/verifysettlement?countryCode={countryCode}&regionCode={regionCode}&settlement={settlement}");

            if (!response.IsSuccessStatusCode)
                return false;

            return response.IsSuccessStatusCode;
        }
    }
}
