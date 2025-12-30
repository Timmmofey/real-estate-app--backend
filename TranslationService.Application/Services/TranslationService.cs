using Classified.Shared.Constants;
using Classified.Shared.DTOs;
using Classified.Shared.Infrastructure.RedisService;
using TranslationService.Domain.Abstractions;

namespace TranslationService.Application.Services
{
    public class GoogleTranslationService: IGoogleTranslationService
    {
        private readonly IGoogleTranslateClient _googleTranslateService;
        private readonly IRedisService _redisService;

        public GoogleTranslationService(IGoogleTranslateClient googleTranslateService, IRedisService redisService)
        {
            _googleTranslateService = googleTranslateService;
            _redisService = redisService;
        }

        public async Task<string?> TranslateAsync(string text, string targetLanguage)
        {
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(targetLanguage))
                throw new ArgumentException("Text or targetLanguage cannot be empty");

            var cacheKey = $"geo:translation:{text.Trim()}";
            var cached = await _redisService.GetAsync(cacheKey);

            if (!string.IsNullOrEmpty(cached))
            {
                return cached;
            }

            var res = await _googleTranslateService.TranslateAsync(text.Trim(), targetLanguage);

            await _redisService.SetAsync(res, cacheKey, TimeSpan.FromDays(15));

            return res;
        }

        public async Task<MultiLanguageTranslationResultDto?> MultipleTranslateAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Text cannot be empty");

            var result = new MultiLanguageTranslationResultDto();

            var allLanguages = Enum.GetValues<Language>();

            if (allLanguages == null || !allLanguages.Any())
                throw new ArgumentException("At least one target language must be provided");


            foreach (var lang in allLanguages)
            {
                var langCode = lang.ToString().ToLowerInvariant();

                var cacheKey = $"geo:multiple-translation:{text.Trim()}:{langCode}";

                var cached = await _redisService.GetAsync(cacheKey);
                if (!string.IsNullOrEmpty(cached))
                {
                    result.Translations[langCode] = cached;
                    continue;
                }

                var translatedText = await _googleTranslateService.TranslateAsync(text.Trim(), langCode);

                await _redisService.SetAsync(cacheKey, translatedText, TimeSpan.FromDays(15));

                result.Translations[langCode] = translatedText;
            }

            return result;
        }
    }
}
