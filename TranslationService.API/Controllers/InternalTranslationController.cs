using Classified.Shared.Constants;
using Classified.Shared.DTOs;
using Classified.Shared.Extensions.ServerJwtAuth;
using Microsoft.AspNetCore.Mvc;
using TranslationService.Domain.Abstractions;

namespace TranslationService.API.Controllers
{
    [Route("internal-api/translation")]
    [ApiController]
    public class InternalTranslationController : ControllerBase
    {
        private readonly IGoogleTranslationService _googleTranslateService;

        public InternalTranslationController(IGoogleTranslationService googleTranslateService)
        {
            _googleTranslateService = googleTranslateService;
        }

        [AuthorizeServerJwt(InternalServices.GeoService)]
        [HttpGet("multiple-translate")]
        public async Task<MultiLanguageTranslationResultDto?> multipleTranslate(string text)
        {
            return await _googleTranslateService.MultipleTranslateAsync(text) ?? null;
        }
    }
}
