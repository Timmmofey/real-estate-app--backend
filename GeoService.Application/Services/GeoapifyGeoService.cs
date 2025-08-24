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

        public GeoapifyGeoService(HttpClient httpClient, IRedisService redis, IConfiguration config)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _redis = redis ?? throw new ArgumentNullException(nameof(redis));
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            _apiKey = config["GEOAPIFY_API_KEY"] ?? throw new InvalidOperationException("GEOAPIFY_API_KEY is not configured");
        }

        public async Task<IReadOnlyList<PlaceSuggestion>> GetSettlementSuggestionsAsync(
            string countryCode,
            string regionCode, 
            string settlement
        )
        {
            const int limit = 10;

            string countryCodeUpper = countryCode.ToUpperInvariant();
            string? regionName = null;

            if (string.IsNullOrWhiteSpace(regionCode) || string.IsNullOrWhiteSpace(countryCode) || string.IsNullOrWhiteSpace(settlement))
                throw new ArgumentException("One or more required parameters are empty");

            if (!GeoConstants.Countries.Any(c => c.Code.Equals(countryCodeUpper, StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException($"Country code '{countryCodeUpper}' not found in constants");

            if (!GeoConstants.RegionsByCountry.TryGetValue(countryCodeUpper, out var reginos))
                throw new ArgumentException($"No regions found for country '{countryCodeUpper}'");

            var region = reginos.FirstOrDefault(r => r.Code.Equals(regionCode, StringComparison.OrdinalIgnoreCase));

            if (region== null)
                return Array.Empty<PlaceSuggestion>();

            regionName = region.Name;
            

            var cacheKey = $"geo:geoapify:settlements:{countryCodeUpper}:{regionName}:{limit}:{settlement}".ToLowerInvariant();
            var cached = await _redis.GetAsync(cacheKey);

            if (!string.IsNullOrEmpty(cached))
                return JsonSerializer.Deserialize<List<PlaceSuggestion>>(cached) ?? new List<PlaceSuggestion>();

            var filters = new List<string>();
            if (!string.IsNullOrWhiteSpace(countryCodeUpper))
                filters.Add($"countrycode:{countryCodeUpper.ToLowerInvariant()}");
            var filterParam = filters.Count > 0 ? $"&filter={string.Join(",", filters)}" : "";

            var url = $"/v1/geocode/autocomplete" +
                      $"?text={Uri.EscapeDataString(settlement)}" +
                      $"&type=city" +
                      $"&limit={limit * 3}" +
                      $"{filterParam}" +
                      $"&apiKey={_apiKey}";

            GeoapifyDtos? response;
            try
            {
                response = await _httpClient.GetFromJsonAsync<GeoapifyDtos>(url, _jsonOptions);
            }
            catch (HttpRequestException)
            {
                return Array.Empty<PlaceSuggestion>();
            }

            if (response?.Features == null || response.Features.Count == 0)
            {
                await _redis.SetAsync(cacheKey, JsonSerializer.Serialize(new List<PlaceSuggestion>()), TimeSpan.FromHours(1));
                return Array.Empty<PlaceSuggestion>();
            }

            var allowedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "city", "town", "village", "hamlet", "locality"
            };

            var suggestions = response.Features
                .Where(f => f.Properties != null)
                .Where(f => {
                    var t = f.Properties!.Type ?? f.Properties!.PlaceType;
                    return string.IsNullOrWhiteSpace(t) || allowedTypes.Contains(t);
                })
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
                .Where(s => (!string.IsNullOrWhiteSpace(s.Region) && s.Region!.IndexOf(regionName, StringComparison.OrdinalIgnoreCase) >= 0))
                .Take(limit)
                .ToList();

            await _redis.SetAsync(cacheKey, JsonSerializer.Serialize(suggestions), TimeSpan.FromHours(12));
            return suggestions;
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
            // 1. Валидация входных данных
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

            // 2. Формируем текст поиска
            var queryText = $"{streetAndNumber} {settlement} {regionCode}";

            // 3. Кэш
            var cacheKey = $"geo:geoapify:postcodes:{countryCodeUpper}:{limit}:{queryText}".ToLowerInvariant();
            var cached = await _redis.GetAsync(cacheKey);
            if (!string.IsNullOrEmpty(cached))
            {
                return JsonSerializer.Deserialize<List<string>>(cached) ?? new List<string>();
            }

            // 4. Формируем URL запроса
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

            // 5. Если ничего не найдено
            if (resp?.Features == null || resp.Features.Count == 0)
            {
                await _redis.SetAsync(cacheKey, JsonSerializer.Serialize(new List<string>()), TimeSpan.FromHours(1));
                return Array.Empty<string>();
            }

            // 6. Собираем уникальные почтовые индексы
            var postcodes = resp.Features
                .Where(f => !string.IsNullOrWhiteSpace(f.Properties?.Postcode))
                .Select(f => f.Properties!.Postcode!)
                .Distinct()
                .ToList();

            // 7. Кэшируем результат
            await _redis.SetAsync(cacheKey, JsonSerializer.Serialize(postcodes), TimeSpan.FromHours(12));

            return postcodes;
        }



        public async Task<GeoapifyResult?> GetValidatedFullAddress(
            string countryCode,
            string regionCode,
            string settlement,
            string street,
            int zipCode
        )
        {
            if (string.IsNullOrWhiteSpace(countryCode)
                || string.IsNullOrWhiteSpace(regionCode)
                || string.IsNullOrWhiteSpace(settlement)
                || string.IsNullOrWhiteSpace(street))
            {
                throw new ArgumentException("One or more required parameters are empty");
            }

            var countryCodeUpper = countryCode.ToUpperInvariant();

            if (!GeoConstants.Countries.Any(c => c.Code.Equals(countryCodeUpper, StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException($"Country code '{countryCodeUpper}' not found in constants");

            if (!GeoConstants.RegionsByCountry.TryGetValue(countryCodeUpper, out var regions))
                throw new ArgumentException($"No regions found for country '{countryCodeUpper}'");

            var region = regions.FirstOrDefault(r => r.Code.Equals(regionCode, StringComparison.OrdinalIgnoreCase));
            if (region == null)
                throw new ArgumentException($"Region code '{regionCode}' not found for country '{countryCodeUpper}'");

            var regionName = region.Name;

            var addressText = $"{street}, {settlement}, {regionName}, {countryCode}, {zipCode}";

            var url = $"/v1/geocode/search" +
                      $"?text={Uri.EscapeDataString(addressText)}" +
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
