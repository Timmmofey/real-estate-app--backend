using GeoService.Domain.Abstractions;
using Microsoft.AspNetCore.Mvc;


namespace GeoService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GeoController : ControllerBase
    {
        private readonly IOpenCageGeoService _openCageGeoService;
        private readonly IGeoapifyGeoService _geoapifyGeoService;
        public GeoController(IOpenCageGeoService geoService, IGeoapifyGeoService geoapifyGeoService)
        {
            _openCageGeoService = geoService;
            _geoapifyGeoService = geoapifyGeoService;
        }

        [HttpGet("suggest-open-cage")]
        public async Task<IActionResult> GetSuggestionsOpenCage(
            [FromQuery] string query,
            [FromQuery] string? countryCode = null,
            [FromQuery] string? stateCode = null,
            [FromQuery] int limit = 10)
        {
            if (string.IsNullOrWhiteSpace(query)) return BadRequest("Query is required");

            var res = await _openCageGeoService.GetSettlementSuggestionsAsync(query, countryCode, stateCode, limit);
            return Ok(res);
        }

        [HttpGet("suggest-post-code-open-cage")]
        public async Task<IActionResult> GetPostCodeSuggestionsOpenCage(
            [FromQuery] string countryCode,
            [FromQuery] string? stateCode,
            [FromQuery] string settlement)
        {
            if (string.IsNullOrWhiteSpace(countryCode) || string.IsNullOrWhiteSpace(stateCode) || string.IsNullOrWhiteSpace(settlement)) return BadRequest("Query is required");

            var res = await _openCageGeoService.GetPostcodeSuggestionsAsync(countryCode, stateCode, settlement);
            return Ok(res);
        }

        // GET /api/Geo/suggest?query=hou&countryCode=US&stateCode=TX&limit=10
        [HttpGet("suggestsettlements")]
        public async Task<IActionResult> GetSuggestions([FromQuery] string query, [FromQuery] string? countryCode, [FromQuery] string? regionCode)
        {
            if (string.IsNullOrWhiteSpace(query)) return BadRequest("query is required");
            var res = await _geoapifyGeoService.GetSettlementSuggestionsAsync(countryCode, regionCode, query);
            return Ok(res);
        }

        [HttpGet("suggestadress")]
        public async Task<IActionResult> GetAdresses([FromQuery] string countryCode, [FromQuery] string stateCode, [FromQuery] string city, [FromQuery] string streetAndNumber)
        {
            if (string.IsNullOrWhiteSpace(streetAndNumber)) return BadRequest("query is required");
            var res = await _geoapifyGeoService.GetAddressSuggestionsAsync(countryCode, stateCode, city, streetAndNumber );
            return Ok(res);
        }
        
        // GET /api/Geo/postcodes?countryCode=US&stateCode=TX&settlement=houston&limit=10
        [HttpGet("postcodes")]
        public async Task<IActionResult> GetPostcodes([FromQuery] string countryCode, [FromQuery] string regionCode, [FromQuery] string settlement, [FromQuery] string streetAndNumber)
        {
            //if (string.IsNullOrWhiteSpace(countryCode) || string.IsNullOrWhiteSpace(stateCode) || string.IsNullOrWhiteSpace(settlement))
            //    return BadRequest("countryCode, stateCode and settlement are required");

            var res = await _geoapifyGeoService.GetPostcodesAsync(countryCode, regionCode, settlement, streetAndNumber);
            return Ok(res);
        }

        [HttpGet("verifyaddress")]
        public async Task<IActionResult> VerifyAddress([FromQuery] string text)
        {
            var res = await _geoapifyGeoService.GetValidatedFullAddress(text);
            return Ok(res);
        }

        [HttpGet("verifysettlement")]
        public async Task<IActionResult> VerifySettlement([FromQuery] string countryCode, [FromQuery] string regionCode, [FromQuery] string settlement)
        {
            var res = await _geoapifyGeoService.GetValidatedSettlement(countryCode, regionCode, settlement);
            return Ok(res);
        }

    }
}
