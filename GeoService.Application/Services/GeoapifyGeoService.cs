using Classified.Shared.DTOs;
using Classified.Shared.Infrastructure.RedisService;
using GeoService.Domain.Abstractions;
using GeoService.Domain.Models;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text.Json;

namespace GeoService.Application.Services
{
    public class GeoapifyGeoService : IGeoapifyGeoService
    {
        private readonly HttpClient _httpClient;
        private readonly IRedisService _redis;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly string _apiKey;
        private readonly ITranslateServiceClient _translateServiceClient;

        public GeoapifyGeoService(HttpClient httpClient, IRedisService redis, IConfiguration config, ITranslateServiceClient translateServiceClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _redis = redis ?? throw new ArgumentNullException(nameof(redis));
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            _apiKey = config["GEOAPIFY_API_KEY"] ?? throw new InvalidOperationException("GEOAPIFY_API_KEY is not configured");
            _translateServiceClient = translateServiceClient;
        }

        public async Task<IReadOnlyList<SettlementSuggestionDto>> GetSettlementSuggestionsAsync(
            string countryCode,
            string regionCode,
            string settlement
        )
        {
            const int limit = 10;

            if (string.IsNullOrWhiteSpace(countryCode) ||
                string.IsNullOrWhiteSpace(regionCode) ||
                string.IsNullOrWhiteSpace(settlement))
                throw new ArgumentException("One or more required parameters are empty");

            string countryCodeUpper = countryCode.ToUpperInvariant();

            if (!GeoConstants.Countries.Any(c => c.Code.Equals(countryCodeUpper, StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException($"Country code '{countryCodeUpper}' not found in constants");

            if (!GeoConstants.RegionsByCountry.TryGetValue(countryCodeUpper, out var regions))
                throw new ArgumentException($"No regions found for country '{countryCodeUpper}'");

            var region = regions.FirstOrDefault(r => r.Code.Equals(regionCode, StringComparison.OrdinalIgnoreCase));
            if (region == null)
                return Array.Empty<SettlementSuggestionDto>();

            string regionName = region.Name;

            var cacheKey = $"geo:settlements:{countryCodeUpper}:{regionName}:{limit}:{settlement}".ToLowerInvariant();
            var cached = await _redis.GetAsync(cacheKey);
            if (!string.IsNullOrWhiteSpace(cached))
                return JsonSerializer.Deserialize<List<SettlementSuggestionDto>>(cached) ?? new List<SettlementSuggestionDto>();


            var url = $"/v1/geocode/autocomplete" +
                      $"?text={Uri.EscapeDataString(settlement)}" +
                      $"&type=city" +
                      $"&limit={limit * 3}" +
                      $"&filter=countrycode:{countryCodeUpper.ToLowerInvariant()}" +
                      $"&apiKey={_apiKey}";

            GeoapifyDtos? response;

            try
            {
                response = await _httpClient.GetFromJsonAsync<GeoapifyDtos>(url, _jsonOptions);
            }
            catch (HttpRequestException)
            {
                return Array.Empty<SettlementSuggestionDto>();
            }

            if (response?.Features == null || response.Features.Count == 0)
            {
                await _redis.SetAsync(cacheKey, "[]", TimeSpan.FromHours(1));
                return Array.Empty<SettlementSuggestionDto>();
            }

            var allowedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "city", "town", "village", "hamlet", "locality"
            };

            var rawSuggestions = response.Features
                .Where(f => f.Properties != null)
                .Where(f =>
                {
                    var t = f.Properties!.Type ?? f.Properties!.PlaceType;
                    return string.IsNullOrWhiteSpace(t) || allowedTypes.Contains(t);
                })
                .Select(f => new
                {
                    DisplayName = f.Properties!.Formatted ?? f.Properties!.Name ?? f.Properties!.City ?? "",
                    Settlement = f.Properties!.City ?? f.Properties!.Name ?? "",
                    Region = f.Properties!.State ?? ""
                })
                .Where(s => !string.IsNullOrWhiteSpace(s.Region) &&
                            s.Region.Contains(regionName, StringComparison.OrdinalIgnoreCase))
                .Take(limit)
                .ToList();


            var result = new List<SettlementSuggestionDto>(rawSuggestions.Count);

            foreach (var s in rawSuggestions)
            {
                var settlementTranslations = await _translateServiceClient.TranslateAsync(s.Settlement);
                var displayNameTranslations = await _translateServiceClient.TranslateAsync(s.DisplayName);

                result.Add(new SettlementSuggestionDto
                {
                    Settlement = s.Settlement,
                    Other_Settlement_Names = settlementTranslations,

                    DisplayName = s.DisplayName,
                    Other_DisplayName_Names = displayNameTranslations
                });
            }


            await _redis.SetAsync(
                cacheKey,
                JsonSerializer.Serialize(result),
                TimeSpan.FromHours(12)
            );

            return result;
        }

        public async Task<IReadOnlyList<PlaceSuggestion>> GetAddressSuggestionsAsync(
            string countryCode,
            string stateCode,
            string settlement,
            string streetAndNumber
        )
        {
            if (string.IsNullOrWhiteSpace(streetAndNumber) ||
                string.IsNullOrWhiteSpace(settlement) ||
                string.IsNullOrWhiteSpace(countryCode))
            {
                return Array.Empty<PlaceSuggestion>();
            }

            const int limit = 30;

            string countryCodeUpper = countryCode.ToUpperInvariant();
            if (!GeoConstants.Countries.Any(c => c.Code.Equals(countryCodeUpper, StringComparison.OrdinalIgnoreCase)))
                return Array.Empty<PlaceSuggestion>();

            var queryText = $"{streetAndNumber} {settlement} {stateCode}";

            var cacheKey = $"geo:geoapify:address:{countryCodeUpper}:{limit}:{queryText}".ToLowerInvariant();
            var cached = await _redis.GetAsync(cacheKey);
            if (!string.IsNullOrEmpty(cached))
            {
                return JsonSerializer.Deserialize<List<PlaceSuggestion>>(cached) ?? new List<PlaceSuggestion>();
            }

            var url = $"/v1/geocode/autocomplete" +
                      $"?text={Uri.EscapeDataString(queryText)}" +
                      $"&filter=countrycode:{countryCodeUpper.ToLowerInvariant()}" +
                      $"&lang=en" +
                      $"&limit={limit}" +
                      $"&apiKey={_apiKey}";

            GeoapifyDtos? resp;
            try
            {
                resp = await _httpClient.GetFromJsonAsync<GeoapifyDtos>(url, _jsonOptions);
            }
            catch (HttpRequestException)
            {
                return Array.Empty<PlaceSuggestion>();
            }

            if (resp?.Features == null || resp.Features.Count == 0)
            {
                await _redis.SetAsync(cacheKey, JsonSerializer.Serialize(new List<PlaceSuggestion>()), TimeSpan.FromHours(1));
                return Array.Empty<PlaceSuggestion>();
            }

            var suggestions = resp.Features
                .Where(f => f.Properties != null)
                .Select(f => new PlaceSuggestion(
                    DisplayName: f.Properties!.Formatted ?? f.Properties!.Name ?? f.Properties!.City ?? "",
                    OSMType: "geoapify",
                    OSMId: 0,
                    Lat: f.Geometry?.Coordinates?.ElementAtOrDefault(1) ?? 0,
                    Lon: f.Geometry?.Coordinates?.ElementAtOrDefault(0) ?? 0,
                    CountryCode: f.Properties!.CountryCode?.ToUpperInvariant() ?? "",
                    Region: f.Properties!.State,
                    County: f.Properties!.County,
                    Settlement: f.Properties!.City ?? f.Properties!.Name,
                    Postcode: f.Properties!.Postcode
                ))
                .ToList();

            await _redis.SetAsync(cacheKey, JsonSerializer.Serialize(suggestions), TimeSpan.FromHours(1));
            return suggestions;
        }




        public async Task<IReadOnlyList<string>> GetPostcodesAsync(
            string countryCode,
            string regionCode,
            string settlement,
            string streetAndNumber
        )
        {
            if (string.IsNullOrWhiteSpace(streetAndNumber) ||
                string.IsNullOrWhiteSpace(settlement) ||
                string.IsNullOrWhiteSpace(countryCode))
            {
                return Array.Empty<string>();
            }

            const int limit = 30;

            string countryCodeUpper = countryCode.ToUpperInvariant();
            if (!GeoConstants.Countries.Any(c => c.Code.Equals(countryCodeUpper, StringComparison.OrdinalIgnoreCase)))
                return Array.Empty<string>();

            var queryText = $"{streetAndNumber} {settlement} {regionCode}";

            var cacheKey = $"geo:geoapify:postcodes:{countryCodeUpper}:{limit}:{queryText}".ToLowerInvariant();
            var cached = await _redis.GetAsync(cacheKey);
            if (!string.IsNullOrEmpty(cached))
            {
                return JsonSerializer.Deserialize<List<string>>(cached) ?? new List<string>();
            }

            var url = $"/v1/geocode/autocomplete" +
                      $"?text={Uri.EscapeDataString(queryText)}" +
                      $"&filter=countrycode:{countryCodeUpper.ToLowerInvariant()}" +
                      $"&lang=en" +
                      $"&limit={limit}" +
                      $"&apiKey={_apiKey}";

            GeoapifyDtos? resp;
            try
            {
                resp = await _httpClient.GetFromJsonAsync<GeoapifyDtos>(url, _jsonOptions);
            }
            catch (HttpRequestException)
            {
                return Array.Empty<string>();
            }

            if (resp?.Features == null || resp.Features.Count == 0)
            {
                await _redis.SetAsync(cacheKey, JsonSerializer.Serialize(new List<string>()), TimeSpan.FromHours(1));
                return Array.Empty<string>();
            }

            var postcodes = resp.Features
                .Where(f => !string.IsNullOrWhiteSpace(f.Properties?.Postcode))
                .Select(f => f.Properties!.Postcode!)
                .Distinct()
                .ToList();

            await _redis.SetAsync(cacheKey, JsonSerializer.Serialize(postcodes), TimeSpan.FromHours(12));

            return postcodes;
        }

        public async Task<GeoapifyResult?> GetValidatedFullAddress(
            string text
        )
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("Required parameters is empty");
            }

            var url = $"/v1/geocode/search" +
                      $"?text={Uri.EscapeDataString(text)}" +
                      $"&format=json" +
                      $"&type=amenity" +
                      $"&apiKey={_apiKey}";

            GeoapifyResponse? response;
            try
            {
                response = await _httpClient.GetFromJsonAsync<GeoapifyResponse>(url, _jsonOptions);
            }
            catch (HttpRequestException ex)
            {
                throw new Exception("Error calling Geoapify API", ex);
            }

            if (response?.Results == null || response.Results.Count == 0)
                throw new InvalidOperationException("Geoapify returned no results for the given address");

            foreach (var r in response.Results)
            {
                var resultType = r.ResultType?.ToLowerInvariant() ?? "";
                var confidence = r.Rank?.Confidence ?? 0.0;
                var confidenceBuilding = r.Rank?.ConfidenceBuildingLevel ?? 0.0;
                var matchType = r.Rank?.MatchType?.ToLowerInvariant() ?? "";

                bool goodType = resultType.Contains("building") || resultType.Contains("street") || resultType.Contains("house") || resultType.Contains("address");
                bool goodConfidence = confidence >= 0.8 || confidenceBuilding >= 0.75;

                if (goodType || goodConfidence || matchType.Contains("full_match") || matchType.Contains("match_by_building"))
                {
                    return r;
                }
            }

            throw new InvalidOperationException("No valid results passed verification checks");
        }

        public async Task<GeoapifyResult?> GetValidatedSettlement(
            string countryCode,
            string regionCode,
            string settlement
        )
        {
            if (string.IsNullOrWhiteSpace(countryCode)
                || string.IsNullOrWhiteSpace(regionCode)
                || string.IsNullOrWhiteSpace(settlement)
                )
                throw new ArgumentException("One or more required parameters are empty");

            var countryCodeUpper = countryCode.ToUpperInvariant();

            if (!GeoConstants.Countries.Any(c => c.Code.Equals(countryCodeUpper, StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException($"Country code '{countryCodeUpper}' not found in constants");

            if (!GeoConstants.RegionsByCountry.TryGetValue(countryCodeUpper, out var regions))
                throw new ArgumentException($"No regions found for country '{countryCodeUpper}'");

            var region = regions.FirstOrDefault(r => r.Code.Equals(regionCode, StringComparison.OrdinalIgnoreCase));
            if (region == null)
                throw new ArgumentException($"Region code '{regionCode}' not found for country '{countryCodeUpper}'");

            var regionName = region.Name;

            var addressText = $"{settlement}, {regionName}, {countryCode}";

            var url = $"/v1/geocode/search" +
                      $"?text={Uri.EscapeDataString(addressText)}" +
                      $"&type=city" +
                      $"&format=json" +
                      $"&apiKey={_apiKey}";

            GeoapifyResponse? response;
            try
            {
                response = await _httpClient.GetFromJsonAsync<GeoapifyResponse>(url, _jsonOptions);
            }
            catch (HttpRequestException ex)
            {
                throw new Exception("Error calling Geoapify API", ex);
            }

            if (response?.Results == null || response.Results.Count == 0)
                throw new InvalidOperationException("Geoapify returned no results for the given address");

            foreach (var r in response.Results)
            {
                if (!string.Equals(r.CountryCode, countryCodeUpper, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!string.IsNullOrWhiteSpace(regionName) &&
                    (string.IsNullOrWhiteSpace(r.State) ||
                     r.State.IndexOf(regionName, StringComparison.OrdinalIgnoreCase) < 0))
                    continue;

                var placeName = r.City ?? r.Village ?? r.Town ?? r.Locality ?? r.Formatted ?? string.Empty;
                if (string.IsNullOrWhiteSpace(placeName) ||
                    placeName.IndexOf(settlement, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                var resultType = r.ResultType?.ToLowerInvariant() ?? "";
                var confidence = r.Rank?.Confidence ?? 0.0;
                var confidenceBuilding = r.Rank?.ConfidenceBuildingLevel ?? 0.0;
                var matchType = r.Rank?.MatchType?.ToLowerInvariant() ?? "";

                bool goodType = resultType.Contains("city") || resultType.Contains("village") || resultType.Contains("town") || resultType.Contains("locality");
                bool goodConfidence = confidence >= 0.8 || confidenceBuilding >= 0.75;

                if (goodType || goodConfidence || matchType.Contains("full_match") || matchType.Contains("match_by_building"))
                {
                    return r;
                }
            }

            throw new InvalidOperationException("No valid results passed verification checks");
        }

        



    }
}
