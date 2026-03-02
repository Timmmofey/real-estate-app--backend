using Classified.Shared.Constants;
using Classified.Shared.Extensions.ServerJwtAuth;
using GeoService.Domain.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace GeoService.API.Controllers
{
    [Route("internal-api/geo")]
    [ApiController]
    public class InternalGeoController : ControllerBase
    {
        private readonly IGeoapifyGeoService _geoapifyGeoService;
        public InternalGeoController(IGeoapifyGeoService geoapifyGeoService)
        {
            _geoapifyGeoService = geoapifyGeoService;
        }

        [AuthorizeServerJwt(InternalServices.UserService)]
        [HttpGet("verifysettlement")]
        public async Task<IActionResult> VerifySettlement([FromQuery] string countryCode, [FromQuery] string regionCode, [FromQuery] string settlement)
        {
            var res = await _geoapifyGeoService.GetValidatedSettlement(countryCode, regionCode, settlement);
            return Ok(res);
        }
    }
}
