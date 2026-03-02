namespace GeoService.Domain.Abstractions
{
    public interface ITranslateServiceClient
    {
        Task<Dictionary<string, string>> TranslateAsync(string text);
    }
}
