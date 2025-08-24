namespace GeoService.Application.DTOs
{
    public record OpenMeteoPlace(
        string Name,
        double Latitude,
        double Longitude,
        string CountryCode,
        string? Admin1,
        string? Admin2,
        int? Population
    );
    public class OpenMeteoResponse
    {
        public List<OpenMeteoPlace>? Results { get; set; }
    }
}
