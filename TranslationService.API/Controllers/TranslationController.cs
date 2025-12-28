using Classified.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using TranslationService.Domain.Abstractions;

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
        public async Task<MultiLanguageTranslationResultDto?> multipleTranslate(string text)
        {
            return await _googleTranslateService.MultipleTranslateAsync(text) ?? null;
        }
    }
}
