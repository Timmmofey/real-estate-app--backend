using Classified.Shared.DTOs;

namespace TranslationService.Domain.Abstractions
{
    public interface IGoogleTranslationService
    {
        Task<string?> TranslateAsync(string text, string targetLanguage);
        Task<MultiLanguageTranslationResultDto> MultipleTranslateAsync(string text);

    }
}
