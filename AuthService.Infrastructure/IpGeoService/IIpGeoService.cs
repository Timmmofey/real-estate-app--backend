namespace AuthService.Infrastructure.IpGeoService
{

    public record GeoInfo(string? CountryIso, string? CountryName, string? Region, string? City, double? Latitude, double? Longitude);

    public interface IIpGeoService
    {
        Task<GeoInfo> LookupAsync(string ip);
    }
}
