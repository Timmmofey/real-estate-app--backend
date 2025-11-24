using Microsoft.AspNetCore.Mvc;
using TranslationService.Domain.Abstractions;
using TranslationService.Domain.Models;

namespace TranslationService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TranslationController : ControllerBase
    {
        private readonly IGoogleTranslationService _googleTranslateService;

        public TranslationController(IGoogleTranslationService googleTranslateService)
        {
            _googleTranslateService = googleTranslateService;
        }

        [HttpGet("translate")]
        public async Task<IActionResult> translate(string text, string targetLanguage) {
            var res = await _googleTranslateService.TranslateAsync(text, targetLanguage);

            return Ok(res);
        }

        [HttpGet("multiple-translate")]
        public async Task<MultiLanguageTranslationResult?> multipleTranslate(string text)
        {
            return await _googleTranslateService.MultipleTranslateAsync(text) ?? null;
        }
    }
}
