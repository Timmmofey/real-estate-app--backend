using Classified.Shared.Constants;
using Classified.Shared.Extensions.ServerJwtAuth;
using GeoService.Domain.Abstractions;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

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

        //[HttpGet("verifysettlement")]
        [AuthorizeServerJwt(InternalServices.UserService)]
        [HttpGet("settlements/verifications")]
        public async Task<IActionResult> VerifySettlement([Required][FromQuery] string countryCode, [Required][FromQuery] string regionCode, [Required][FromQuery] string settlement)
        {
            var res = await _geoapifyGeoService.GetValidatedSettlement(countryCode, regionCode, settlement);
            return Ok(res);
        }
    }
}
