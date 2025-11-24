using System.Text.Json;
using TranslationService.Domain.Abstractions;
using TranslationService.Domain.Models;

namespace TranslationService.Infrastructure.GoogleTranslate
{
    public class GoogleTranslateClient: IGoogleTranslateClient
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public GoogleTranslateClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        private string BuildTranslateUrl(string text, string targetLanguage)
        {
            return $"https://translate.googleapis.com/translate_a/single?client=gtx&sl=auto&tl={targetLanguage.ToLower()}&dt=t&q={Uri.EscapeDataString(text)}";
        }

        public async Task<string> TranslateAsync(string text, string targetLanguage)
        {
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrEmpty(targetLanguage))
                throw new ArgumentException("One or more required parameters are empty");

            var url = BuildTranslateUrl(text, targetLanguage);

            var responseString = await _httpClient.GetStringAsync(url);
            using var doc = JsonDocument.Parse(responseString);
            var root = doc.RootElement;

            var sentences = root[0].EnumerateArray();
            var translatedText = string.Concat(sentences.Select(s => s[0].GetString()));

            return translatedText;
        }

        public async Task<MultiLanguageTranslationResult> MultipleTranslateAsync(string text, IEnumerable<string> targetLanguages)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Text cannot be empty");

            if (targetLanguages == null || !targetLanguages.Any())
                throw new ArgumentException("At least one target language must be provided");

            var result = new MultiLanguageTranslationResult();

            foreach (var targetLanguage in targetLanguages)
            {
                var url = BuildTranslateUrl(text, targetLanguage);

                var responseString = await _httpClient.GetStringAsync(url);
                using var doc = JsonDocument.Parse(responseString);
                var root = doc.RootElement;

                var sentences = root[0].EnumerateArray();
                var translatedText = string.Concat(sentences.Select(s => s[0].GetString() ?? string.Empty));

                result.Translations[targetLanguage] = translatedText;
            }

            return result;
        }

        
    }
}
