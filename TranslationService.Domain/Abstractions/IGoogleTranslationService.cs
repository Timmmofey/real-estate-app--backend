using Classified.Shared.Constants;
using TranslationService.Domain.Models;

namespace TranslationService.Domain.Abstractions
{
    public interface IGoogleTranslationService
    {
        Task<string?> TranslateAsync(string text, string targetLanguage);
        Task<MultiLanguageTranslationResult> MultipleTranslateAsync(string text);

    }
}
