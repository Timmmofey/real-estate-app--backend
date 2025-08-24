namespace GeoService.Domain.Models
{
    public record PlaceSuggestion(
        string DisplayName,
        string OSMType,
        long OSMId,
        double Lat,
        double Lon,
        string CountryCode,
        string? Region,
        string? County,
        string? Settlement,
        string? Postcode
    );
}
