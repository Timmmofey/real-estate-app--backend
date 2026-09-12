using Classified.Shared.Constants;
using Classified.Shared.DTOs;
using Classified.Shared.Extensions.ServerJwtAuth;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using TranslationService.Domain.Abstractions;

namespace TranslationService.API.Controllers
{
    [Route("internal-api/translations")]
    [ApiController]
    public class InternalTranslationController : ControllerBase
    {
        private readonly IGoogleTranslationService _googleTranslateService;

        public InternalTranslationController(IGoogleTranslationService googleTranslateService)
        {
            _googleTranslateService = googleTranslateService;
        }

        [AuthorizeServerJwt(InternalServices.GeoService)]
        [HttpGet("multiple")]
        public async Task<MultiLanguageTranslationResultDto?> multipleTranslate([Required] string text)
        {
            return await _googleTranslateService.MultipleTranslateAsync(text) ?? null;
        }
    }
}
