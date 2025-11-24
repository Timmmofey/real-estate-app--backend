using TranslationService.Domain.Models;

namespace TranslationService.Domain.Abstractions
{
    public interface IDeepLTranslationService
    {
        Task<string> TranslateAsync(string text, string targetLang);
        Task<Dictionary<string, string>> TranslateToMultipleAsync(string text, IEnumerable<string> targetLangs);
    }
}
