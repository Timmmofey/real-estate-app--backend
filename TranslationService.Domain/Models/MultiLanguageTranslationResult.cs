namespace TranslationService.Domain.Models
{
    public class MultiLanguageTranslationResult
    {
        public Dictionary<string, string> Translations { get; set; } = new();
    }

}
