using Classified.Shared.Infrastructure.RedisService;
using GeoService.Domain.Abstractions;
using GeoService.Domain.Models;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text.Json;
using static GeoService.Application.DTOs.OpenCageDtos;

namespace GeoService.Application.Services
{
    public class OpenCageGeoService : IOpenCageGeoService
    {
        private readonly HttpClient _http;
        private readonly IRedisService _redis;
        private readonly string _apiKey;
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public OpenCageGeoService(HttpClient httpClient, IRedisService redis, IConfiguration config)
        {
            _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _redis = redis ?? throw new ArgumentNullException(nameof(redis));
            _apiKey = config["OpenCage:ApiKey"] ?? throw new ArgumentNullException("OpenCage:ApiKey");
        }

        public async Task<IReadOnlyList<PlaceSuggestion>> GetSettlementSuggestionsAsync(
            string query,
            string? countryCode = null,
            string? stateCode = null,
            int limit = 10)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Array.Empty<PlaceSuggestion>();

            if (countryCode != null && !GeoConstants.Countries.Any(c =>
                    c.Code.Equals(countryCode, StringComparison.OrdinalIgnoreCase)))
                return Array.Empty<PlaceSuggestion>();

            string? stateName = null;
            if (!string.IsNullOrWhiteSpace(stateCode) && !string.IsNullOrWhiteSpace(countryCode))
            {
                if (GeoConstants.RegionsByCountry.TryGetValue(countryCode.ToUpperInvariant(), out var regions))
                {
                    var reg = regions.FirstOrDefault(r => r.Code.Equals(stateCode, StringComparison.OrdinalIgnoreCase));
                    if (reg == null)
                        return Array.Empty<PlaceSuggestion>();
                    stateName = reg.Name;
                }
            }

            var cacheKey = $"geo:opencage:suggest:{countryCode}:{stateCode}:{limit}:{query}".ToLowerInvariant();
            var cached = await _redis.GetAsync(cacheKey);
            if (!string.IsNullOrEmpty(cached))
            {
                return JsonSerializer.Deserialize<List<PlaceSuggestion>>(cached) ?? new List<PlaceSuggestion>();
            }

            var url = $"https://api.opencagedata.com/geocode/v1/json?q={Uri.EscapeDataString(query)}" +
                      $"&key={_apiKey}" +
                      $"&limit=10000" +
                      "&no_annotations=1";

            if (!string.IsNullOrWhiteSpace(countryCode))
                url += $"&countrycode={Uri.EscapeDataString(countryCode)}";

            var response = await _http.GetFromJsonAsync<OpenCageResponse>(url, _jsonOptions);

            if (response?.Results == null || response.Results.Count == 0)
            {
                await _redis.SetAsync(cacheKey, JsonSerializer.Serialize(new List<PlaceSuggestion>()), TimeSpan.FromHours(1));
                return Array.Empty<PlaceSuggestion>();
            }

            var filtered = response.Results
                .Where(r =>
                    string.IsNullOrWhiteSpace(stateName) ||
                    (!string.IsNullOrWhiteSpace(r.Components.State) &&
                     r.Components.State.Equals(stateName, StringComparison.OrdinalIgnoreCase)))
                .Select(r => new PlaceSuggestion(
                    DisplayName: r.Formatted,
                    OSMType: "opencage",
                    OSMId: 0,
                    Lat: r.Geometry.Lat,
                    Lon: r.Geometry.Lng,
                    CountryCode: r.Components.CountryCode?.ToUpperInvariant() ?? "",
                    Region: r.Components.State,
                    County: r.Components.County,
                    Settlement: r.Components.City ?? r.Components.Town ?? r.Components.Village,
                    Postcode: r.Components.Postcode
                ))
                .Take(10)
                .ToList();

            await _redis.SetAsync(cacheKey, JsonSerializer.Serialize(filtered), TimeSpan.FromHours(12));

            return filtered;
        }

        public async Task<IReadOnlyList<string>> GetPostcodeSuggestionsAsync(
            string countryCode,
            string? stateCode,
            string city)
        {
            if (string.IsNullOrWhiteSpace(countryCode) || string.IsNullOrWhiteSpace(city))
                return Array.Empty<string>();

            string? stateName = null;
            if (!string.IsNullOrWhiteSpace(stateCode))
            {
                if (GeoConstants.RegionsByCountry.TryGetValue(countryCode.ToUpperInvariant(), out var regions))
                {
                    var reg = regions.FirstOrDefault(r => r.Code.Equals(stateCode, StringComparison.OrdinalIgnoreCase));
                    if (reg != null)
                        stateName = reg.Name;
                }
            }

            var cacheKey = $"geo:opencage:postcode:{countryCode}:{stateName}:{city}".ToLowerInvariant();
            var cached = await _redis.GetAsync(cacheKey);
            if (!string.IsNullOrEmpty(cached))
                return JsonSerializer.Deserialize<List<string>>(cached) ?? new List<string>();

            var queryParts = new List<string> { city };
            if (!string.IsNullOrWhiteSpace(stateName)) queryParts.Add(stateName);
            queryParts.Add(countryCode);

            var url = $"geocode/v1/json?q={Uri.EscapeDataString(string.Join(", ", queryParts))}&key={_apiKey}&limit=50";

            var response = await _http.GetFromJsonAsync<OpenCageResponse>(url, _jsonOptions);
            if (response?.Results == null || response.Results.Count == 0)
            {
                await _redis.SetAsync(cacheKey, JsonSerializer.Serialize(new List<string>()), TimeSpan.FromHours(1));
                return Array.Empty<string>();
            }

            var postcodes = response.Results
                .Where(r =>
                    (r.Components.City == city ||
                     r.Components.Town == city ||
                     r.Components.Village == city ||
                     r.Components.Hamlet == city) &&
                    (stateName == null || r.Components.State == stateName) &&
                    r.Components.CountryCode.Equals(countryCode, StringComparison.OrdinalIgnoreCase))
                .Select(r => r.Components.Postcode)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct()
                .ToList();



            await _redis.SetAsync(cacheKey, JsonSerializer.Serialize(postcodes), TimeSpan.FromHours(1));

            return postcodes;
        }

    }
}
