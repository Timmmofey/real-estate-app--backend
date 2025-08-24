using System.Text.Json.Serialization;

namespace GeoService.Application.DTOs
{
    public class OpenCageDtos
    {
        public record OpenCageResponse(
            [property: JsonPropertyName("results")] List<OpenCageResult> Results,
            [property: JsonPropertyName("status")] object? Status
        );

        public record OpenCageResult(
            [property: JsonPropertyName("formatted")] string Formatted,
            [property: JsonPropertyName("geometry")] OpenCageGeometry Geometry,
            [property: JsonPropertyName("components")] OpenCageComponents Components,
            [property: JsonPropertyName("annotations")] object? Annotations
        );

        public record OpenCageGeometry(
            [property: JsonPropertyName("lat")] double Lat,
            [property: JsonPropertyName("lng")] double Lng
        );

        public record OpenCageComponents(
            [property: JsonPropertyName("country_code")] string? CountryCode,
            [property: JsonPropertyName("state")] string? State,
            [property: JsonPropertyName("county")] string? County,
            [property: JsonPropertyName("city")] string? City,
            [property: JsonPropertyName("town")] string? Town,
            [property: JsonPropertyName("village")] string? Village,
            [property: JsonPropertyName("postcode")] string? Postcode,
            [property: JsonPropertyName("state_code")] string? StateCode,
            [property: JsonPropertyName("hamlet")] string? Hamlet

        );
    }
}
