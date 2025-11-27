using System.Text.Json.Serialization;

namespace Classified.Shared.DTOs
{
    public class GeoapifyDtos
    {
        [JsonPropertyName("features")]
        public List<GeoapifyFeature>? Features { get; set; }
    }

    //public class GeoapifySuggestionsDto
    //{
    //    // Некоторые ответные форматы используют "results", некоторые — "features"
    //    [JsonPropertyName("results")]
    //    public List<GeoapifyResult>? Results { get; set; }
    //}

    public class GeoapifyFeature
    {
        [JsonPropertyName("geometry")]
        public GeoapifyGeometry? Geometry { get; set; }

        [JsonPropertyName("properties")]
        public GeoapifyProperties? Properties { get; set; }
    }

    public class GeoapifyGeometry
    {
        [JsonPropertyName("coordinates")]
        public double[]? Coordinates { get; set; } // [lon, lat]
    }

    public class GeoapifyProperties
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("formatted")]
        public string? Formatted { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("country_code")]
        public string? CountryCode { get; set; }

        [JsonPropertyName("state")]
        public string? State { get; set; }

        [JsonPropertyName("county")]
        public string? County { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("town")]
        public string? Town { get; set; }
        [JsonPropertyName("village")]
        public string? Village { get; set; }

        [JsonPropertyName("locality")]
        public string? Locality { get; set; }

        [JsonPropertyName("postcode")]
        public string? Postcode { get; set; }

        [JsonPropertyName("result_type")]
        public string? ResultType { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("place_type")]
        public string? PlaceType { get; set; }

        [JsonPropertyName("rank")]
        public GeoapifyRank? Rank { get; set; }
    }


    public class GeoapifyResponse
    {
        [JsonPropertyName("results")]
        public List<GeoapifyResult> Results { get; set; } = new();

        [JsonPropertyName("query")]
        public GeoapifyQuery Query { get; set; }
    }

    public class GeoapifyResult
    {
        [JsonPropertyName("datasource")]
        public GeoapifyDatasource Datasource { get; set; }

        [JsonPropertyName("country")]
        public string Country { get; set; }

        [JsonPropertyName("country_code")]
        public string CountryCode { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; }

        [JsonPropertyName("county")]
        public string County { get; set; }

        [JsonPropertyName("city")]
        public string City { get; set; }

        [JsonPropertyName("village")]
        public string? Village { get; set; }

        [JsonPropertyName("town")]
        public string? Town { get; set; }

        [JsonPropertyName("locality")]
        public string? Locality { get; set; }

        [JsonPropertyName("postcode")]
        public string Postcode { get; set; }

        [JsonPropertyName("street")]
        public string Street { get; set; }

        [JsonPropertyName("housenumber")]
        public string HouseNumber { get; set; }

        [JsonPropertyName("iso3166_2")]
        public string Iso3166_2 { get; set; }

        [JsonPropertyName("lon")]
        public double Lon { get; set; }

        [JsonPropertyName("lat")]
        public double Lat { get; set; }

        [JsonPropertyName("state_code")]
        public string StateCode { get; set; }

        [JsonPropertyName("result_type")]
        public string ResultType { get; set; }

        [JsonPropertyName("formatted")]
        public string Formatted { get; set; }

        [JsonPropertyName("address_line1")]
        public string AddressLine1 { get; set; }

        [JsonPropertyName("address_line2")]
        public string AddressLine2 { get; set; }

        [JsonPropertyName("timezone")]
        public GeoapifyTimezone Timezone { get; set; }

        [JsonPropertyName("plus_code")]
        public string PlusCode { get; set; }

        [JsonPropertyName("rank")]
        public GeoapifyRank Rank { get; set; }

        [JsonPropertyName("place_id")]
        public string PlaceId { get; set; }

        [JsonPropertyName("bbox")]
        public GeoapifyBbox Bbox { get; set; }
    }

    public class GeoapifyDatasource
    {
        [JsonPropertyName("sourcename")]
        public string SourceName { get; set; }

        [JsonPropertyName("attribution")]
        public string Attribution { get; set; }

        [JsonPropertyName("license")]
        public string License { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }
    }

    public class GeoapifyTimezone
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("offset_STD")]
        public string OffsetStd { get; set; }

        [JsonPropertyName("offset_STD_seconds")]
        public int OffsetStdSeconds { get; set; }

        [JsonPropertyName("offset_DST")]
        public string OffsetDst { get; set; }

        [JsonPropertyName("offset_DST_seconds")]
        public int OffsetDstSeconds { get; set; }

        [JsonPropertyName("abbreviation_STD")]
        public string AbbreviationStd { get; set; }

        [JsonPropertyName("abbreviation_DST")]
        public string AbbreviationDst { get; set; }
    }

    public class GeoapifyRank
    {
        [JsonPropertyName("importance")]
        public double Importance { get; set; }

        [JsonPropertyName("popularity")]
        public double Popularity { get; set; }

        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }

        [JsonPropertyName("confidence_city_level")]
        public double ConfidenceCityLevel { get; set; }

        [JsonPropertyName("confidence_street_level")]
        public double ConfidenceStreetLevel { get; set; }

        [JsonPropertyName("confidence_building_level")]
        public double? ConfidenceBuildingLevel { get; set; }

        [JsonPropertyName("match_type")]
        public string? MatchType { get; set; }
    }

    public class GeoapifyBbox
    {
        [JsonPropertyName("lon1")]
        public double Lon1 { get; set; }

        [JsonPropertyName("lat1")]
        public double Lat1 { get; set; }

        [JsonPropertyName("lon2")]
        public double Lon2 { get; set; }

        [JsonPropertyName("lat2")]
        public double Lat2 { get; set; }
    }

    public class GeoapifyQuery
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("parsed")]
        public GeoapifyParsed Parsed { get; set; }
    }

    public class GeoapifyParsed
    {
        [JsonPropertyName("housenumber")]
        public string HouseNumber { get; set; }

        [JsonPropertyName("street")]
        public string Street { get; set; }

        [JsonPropertyName("postcode")]
        public string Postcode { get; set; }

        [JsonPropertyName("city")]
        public string City { get; set; }

        [JsonPropertyName("expected_type")]
        public string ExpectedType { get; set; }
    }

}
