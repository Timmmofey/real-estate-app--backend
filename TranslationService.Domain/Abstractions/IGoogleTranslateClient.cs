using TranslationService.Domain.Models;

namespace TranslationService.Domain.Abstractions
{
    public interface IGoogleTranslateClient
    {
        Task<string> TranslateAsync(string text, string targetLanguage);
        Task<MultiLanguageTranslationResult> MultipleTranslateAsync(string text, IEnumerable<string> targetLanguages);
    }
}
