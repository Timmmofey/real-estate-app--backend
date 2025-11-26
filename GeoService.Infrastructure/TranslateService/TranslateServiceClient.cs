using Classified.Shared.DTOs;
using GeoService.Domain.Abstractions;
using System.Net.Http;
using System.Net.Http.Json;

namespace GeoService.Infrastructure.TranslateService
{
    public class TranslateServiceClient: ITranslateServiceClient
    {
        private readonly HttpClient _httpClient;


        public TranslateServiceClient(HttpClient http)
        {
            _httpClient = http;
        }

        public async Task<Dictionary<string, string>> TranslateAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new();


            string url = $"http://localhost:5229/api/Translation/multiple-translate?text={Uri.EscapeDataString(text)}";

            try
            {
                var r = await _httpClient.GetFromJsonAsync<MultiLanguageTranslationResultDto>(url);

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
