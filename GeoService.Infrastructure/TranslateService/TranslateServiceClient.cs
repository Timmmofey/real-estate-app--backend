using Classified.Shared.Constants;
using Classified.Shared.DTOs;
using Classified.Shared.Extensions;
using Classified.Shared.Infrastructure.MicroserviceJwt;
using GeoService.Domain.Abstractions;
using Microsoft.AspNetCore.WebUtilities;
using System.Net.Http.Json;

namespace GeoService.Infrastructure.TranslateService
{
    public class TranslateServiceClient: ITranslateServiceClient
    {
        private readonly HttpClient _http;
        private readonly IMicroserviceJwtProvider _microserviceJwtProvider;


        public TranslateServiceClient(HttpClient http, IMicroserviceJwtProvider microserviceJwtProvider)
        {
            _http = http;
            _microserviceJwtProvider = microserviceJwtProvider;
        }

        public async Task<Dictionary<string, string>> TranslateAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new();

            _http.SetServerJwt(_microserviceJwtProvider, InternalServices.GeoService, InternalServices.TranslationService);

            var queryParams = new Dictionary<string, string?>
            {
                { "text" ,  text }
            };

            var url = QueryHelpers.AddQueryString("internal-api/translation/multiple-translate", queryParams);

            try
            {
                var r = await _http.GetFromJsonAsync<MultiLanguageTranslationResultDto>(url);

                var dict = new Dictionary<string, string>();
                if (r?.Translations != null)
                {
                    foreach (var kvp in r.Translations)
                    {
                        if (!string.Equals(kvp.Value, text, StringComparison.OrdinalIgnoreCase))
                            dict[kvp.Key] = kvp.Value;
                        else
                            dict[kvp.Key] = kvp.Value;
                    }
                }

                return dict;
            }
            catch
            {
                return new Dictionary<string, string> { { "en", text } };
            }
        }
    }
}
