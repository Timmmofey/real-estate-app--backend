using Classified.Shared.Infrastructure.RedisService;
using GeoService.Application.DTOs;
using GeoService.Domain.Abstractions;
using GeoService.Domain.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace GeoService.Application.Services
{
    public class OpenMeteoGeoService : IMeteoGeoService
    {
        private readonly HttpClient _httpClient;
        private readonly IRedisService _redis;
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public OpenMeteoGeoService(HttpClient httpClient, IRedisService redis)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        }

        public async Task<IReadOnlyList<PlaceSuggestion>> GetMeteoGeoServiceSettlementSuggestionsAsync(
            string query,
            string? countryCode = null,
            string? regionCode = null,
            int limit = 10)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Array.Empty<PlaceSuggestion>();

            if (!string.IsNullOrWhiteSpace(countryCode) &&
                !GeoConstants.Countries.Any(c => c.Code.Equals(countryCode, StringComparison.OrdinalIgnoreCase)))
            {
                return Array.Empty<PlaceSuggestion>();
            }

            string? stateName = null;
            if (!string.IsNullOrWhiteSpace(regionCode) && !string.IsNullOrWhiteSpace(countryCode))
            {
                if (GeoConstants.RegionsByCountry.TryGetValue(countryCode.ToUpperInvariant(), out var regions))
                {
                    var reg = regions.FirstOrDefault(r => r.Code.Equals(regionCode, StringComparison.OrdinalIgnoreCase));
                    if (reg == null)
                        return Array.Empty<PlaceSuggestion>();
                    stateName = reg.Name;
                }
            }

            var cacheKey = $"geo:openmeteo:suggest:{countryCode}:{regionCode}:{limit}:{query}".ToLowerInvariant();
            var cached = await _redis.GetAsync(cacheKey);
            if (!string.IsNullOrEmpty(cached))
            {
                return JsonSerializer.Deserialize<List<PlaceSuggestion>>(cached) ?? new List<PlaceSuggestion>();
            }

            var url = $"v1/search?name={Uri.EscapeDataString(query)}&count={limit}&language=en&format=json";
            if (!string.IsNullOrWhiteSpace(countryCode))
                url += $"&country={Uri.EscapeDataString(countryCode)}";

            OpenMeteoResponse? apiResponse;
            try
            {
                apiResponse = await _httpClient.GetFromJsonAsync<OpenMeteoResponse>(url, _jsonOptions);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine("OpenMeteo request failed: " + ex.Message);
                return Array.Empty<PlaceSuggestion>();
            }

            var results = apiResponse?.Results;
            if (results == null || results.Count == 0)
            {
                await _redis.SetAsync(cacheKey, JsonSerializer.Serialize(new List<PlaceSuggestion>()), TimeSpan.FromHours(1));
                return Array.Empty<PlaceSuggestion>();
            }

            if (!string.IsNullOrWhiteSpace(stateName))
            {
                results = results
                    .Where(r => !string.IsNullOrWhiteSpace(r.Admin1) &&
                                r.Admin1.Equals(stateName, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var suggestions = results.Select(r => new PlaceSuggestion(
                DisplayName: $"{r.Name}, {r.Admin1}, {r.CountryCode}",
                OSMType: "place",
                OSMId: 0,
                Lat: r.Latitude,
                Lon: r.Longitude,
                CountryCode: r.CountryCode,
                Region: r.Admin1,
                County: r.Admin2,
                Settlement: r.Name,
                Postcode: null
            )).ToList();

            await _redis.SetAsync(cacheKey, JsonSerializer.Serialize(suggestions), TimeSpan.FromHours(1));

            return suggestions;
        }
    }

}