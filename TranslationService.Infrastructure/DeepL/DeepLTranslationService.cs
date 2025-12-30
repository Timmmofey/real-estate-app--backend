//using TranslationService.Domain.Abstractions;
//using DeepL;

//namespace TranslationService.Infrastructure.DeepL
//{
//    public class DeepLTranslationService: IDeepLTranslationService
//    {
//        private readonly DeepLClient _client;

//        public DeepLTranslationService(string apiKey)
//        {
//            _client = new DeepLClient(apiKey);
//        }

//        public async Task<string> TranslateAsync(string text, string targetLang)
//        {
//            var result = await _client.TranslateTextAsync(text,null, targetLang);
//            return result.Text;
//        }

//        public async Task<Dictionary<string, string>> TranslateToMultipleAsync(
//            string text,
//            IEnumerable<string> targetLangs)
//        {
//            var result = new Dictionary<string, string>();

//            foreach (var lang in targetLangs)
//            {
//                var translation = await _client.TranslateTextAsync(
//                    text,
//                    null,
//                    lang
//                );

//                result[lang] = translation.Text;
//            }

//            return result;
//        }

//    }
//}
