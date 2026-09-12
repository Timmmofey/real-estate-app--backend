using Classified.Shared.Constants;
using Classified.Shared.Infrastructure.MicroserviceJwt;
using Classified.Shared.Libs;
using Microsoft.AspNetCore.WebUtilities;

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

            var queryParams = new Dictionary<string, string?>
            {
                { "countryCode", countryCode },
                { "regionCode", regionCode },
                { "settlement", settlement },
            };

            var url = QueryHelpers.AddQueryString("internal-api/Geo/settlements/verifications", queryParams);

            var response = await _http.GetAsync(url);

            return response.IsSuccessStatusCode;
        }
    }
}
