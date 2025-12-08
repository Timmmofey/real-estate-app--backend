using Classified.Shared.DTOs;
using Classified.Shared.Infrastructure.RedisService;
using GeoService.Domain.Abstractions;
using GeoService.Domain.Models;
using Microsoft.Extensions.Configuration;
using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

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



        //public async Task<IReadOnlyList<SettlementSuggestionDto>> GetSettlementSuggestionsAsync(
        //    string countryCode,
        //    string regionCode,
        //    string settlement
        //)
        //{
        //    const int limit = 10;

        //    if (string.IsNullOrWhiteSpace(countryCode) ||
        //        string.IsNullOrWhiteSpace(regionCode) ||
        //        string.IsNullOrWhiteSpace(settlement))
        //        throw new ArgumentException("One or more required parameters are empty");

        //    string countryCodeUpper = countryCode.ToUpperInvariant();

        //    if (!GeoConstants.Countries.Any(c => c.Code.Equals(countryCodeUpper, StringComparison.OrdinalIgnoreCase)))
        //        throw new ArgumentException($"Country code '{countryCodeUpper}' not found in constants");

        //    if (!GeoConstants.RegionsByCountry.TryGetValue(countryCodeUpper, out var regions))
        //        throw new ArgumentException($"No regions found for country '{countryCodeUpper}'");

        //    var region = regions.FirstOrDefault(r => r.Code.Equals(regionCode, StringComparison.OrdinalIgnoreCase));
        //    if (region == null)
        //        return Array.Empty<SettlementSuggestionDto>();

        //    string regionName = region.Name;

        //    var cacheKey = $"geo:settlements:{countryCodeUpper}:{regionName}:{limit}:{settlement}".ToLowerInvariant();
        //    var cached = await _redis.GetAsync(cacheKey);
        //    if (!string.IsNullOrWhiteSpace(cached))
        //        return JsonSerializer.Deserialize<List<SettlementSuggestionDto>>(cached) ?? new List<SettlementSuggestionDto>();


        //    var url = $"/v1/geocode/autocomplete" +
        //              $"?text={Uri.EscapeDataString($"{settlement} {regionCode} {countryCode}")}" +
        //              $"&type=city" +
        //              $"&limit={limit * 3}" +
        //              $"&filter=countrycode:{countryCodeUpper.ToLowerInvariant()}" +
        //              $"&apiKey={_apiKey}";

        //    GeoapifyDtos? response;

        //    try
        //    {
        //        response = await _httpClient.GetFromJsonAsync<GeoapifyDtos>(url, _jsonOptions);
        //    }
        //    catch (HttpRequestException)
        //    {
        //        return Array.Empty<SettlementSuggestionDto>();
        //    }

        //    if (response?.Features == null || response.Features.Count == 0)
        //    {
        //        await _redis.SetAsync(cacheKey, "[]", TimeSpan.FromHours(1));
        //        return Array.Empty<SettlementSuggestionDto>();
        //    }

        //    var allowedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        //    {
        //        "city", "town", "village", "hamlet", "locality", "postcode"
        //    };

        //    var rawSuggestions = response.Features
        //        .Where(f => f.Properties != null)
        //        .Where(f => f.Properties!.Rank != null && f.Properties!.Rank.ConfidenceCityLevel >= 0.9)
        //        .Where(f =>
        //        {
        //            var t = f.Properties!.ResultType ?? f.Properties!.Type ?? f.Properties!.PlaceType;
        //            return !string.IsNullOrWhiteSpace(t) && allowedTypes.Contains(t);
        //        })
        //        .Select(f => new
        //        {
        //            DisplayName = f.Properties!.Formatted ?? f.Properties!.Name ?? f.Properties!.City ?? "",
        //            Settlement = f.Properties!.City ?? f.Properties!.Name ?? "",
        //            Region = f.Properties!.State ?? ""
        //        })
        //        .Where(s => !string.IsNullOrWhiteSpace(s.Region) &&
        //                    s.Region.Contains(regionName, StringComparison.OrdinalIgnoreCase))
        //        .Take(limit)
        //        .ToList();


        //    var result = new List<SettlementSuggestionDto>(rawSuggestions.Count);

        //    foreach (var s in rawSuggestions)
        //    {
        //        var displayNameTranslations = await _translateServiceClient.TranslateAsync(s.DisplayName);

        //        // функция для извлечения только названия поселения
        //        string ExtractSettlementName(string fullName)
        //        {
        //            if (string.IsNullOrWhiteSpace(fullName))
        //                return string.Empty;

        //            var parts = fullName.Split(',');
        //            return parts[0].Trim();
        //        }

        //        var settlementTranslations = displayNameTranslations.ToDictionary(
        //            kvp => kvp.Key,
        //            kvp => ExtractSettlementName(kvp.Value)
        //        );

        //        result.Add(new SettlementSuggestionDto
        //        {
        //            Settlement = s.Settlement,
        //            Other_Settlement_Names = settlementTranslations,

        //            DisplayName = s.DisplayName,
        //            Other_DisplayName_Names = displayNameTranslations
        //        });
        //    }


        //    await _redis.SetAsync(
        //        cacheKey,
        //        JsonSerializer.Serialize(result),
        //        TimeSpan.FromHours(12)
        //    );

        //    return result;
        //}



        public async Task<IReadOnlyList<SettlementSuggestionDto>> GetSettlementSuggestionsAsync(
            string countryCode,
            string regionCode,
            string settlement
        )
        {
            // === CONFIG / LIMITS ===
            int limit = 10;
            // alias / performance tuning (tweak for your system)
            int AliasMinPrefixLen = 2;
            int AliasMaxPrefixLen = 30;
            int MaxPrefixesPerName = 20;
            int MaxAliasListSize = 200;
            int ParallelFetchLimit = 25;
            TimeSpan AliasTtl = TimeSpan.FromHours(12);
            TimeSpan BaseTtl = TimeSpan.FromHours(12);

            // --- Local helper functions (capture above variables) ---

            string NormalizeForKey(string s)
            {
                if (string.IsNullOrWhiteSpace(s)) return string.Empty;
                s = s.Trim().ToLowerInvariant();

                var normalized = s.Normalize(NormalizationForm.FormD);
                var sb = new StringBuilder();
                foreach (var ch in normalized)
                {
                    var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
                    if (cat != UnicodeCategory.NonSpacingMark)
                        sb.Append(ch);
                }

                // replace punctuation except spaces with space, collapse spaces
                var compact = Regex.Replace(sb.ToString(), @"[^\p{L}\p{N}\s]", " ");
                compact = Regex.Replace(compact, @"\s+", " ").Trim();
                return compact;
            }

            IEnumerable<string> GenerateLimitedPrefixes(string name)
            {
                var norm = NormalizeForKey(name);
                if (string.IsNullOrWhiteSpace(norm)) yield break;

                // whole phrase prefixes (from min len up to limited length)
                int maxLen = Math.Min(AliasMaxPrefixLen, norm.Length);
                int count = 0;
                for (int len = AliasMinPrefixLen; len <= maxLen && count < MaxPrefixesPerName; len++)
                {
                    yield return norm.Substring(0, len);
                    count++;
                }

                // отдельные слова: добавляем первые N prefixes для каждого слова (если осталось место)
                if (count < MaxPrefixesPerName)
                {
                    var words = norm.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var w in words)
                    {
                        int wordMax = Math.Min(w.Length, AliasMaxPrefixLen);
                        for (int l = AliasMinPrefixLen; l <= wordMax && count < MaxPrefixesPerName; l++)
                        {
                            yield return w.Substring(0, l);
                            count++;
                        }
                        if (count >= MaxPrefixesPerName) break;
                    }
                }
            }

            IEnumerable<SettlementSuggestionDto> FilterAndDedup(IEnumerable<SettlementSuggestionDto> items, string normalizedQuery)
            {
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var it in items)
                {
                    bool matched = false;
                    // settlement exact/prefix match
                    if (!string.IsNullOrWhiteSpace(it.Settlement) && NormalizeForKey(it.Settlement).StartsWith(normalizedQuery))
                        matched = true;

                    // translations settlement
                    if (!matched && it.Other_Settlement_Names != null)
                    {
                        foreach (var v in it.Other_Settlement_Names.Values)
                        {
                            if (!string.IsNullOrWhiteSpace(v) && NormalizeForKey(v).StartsWith(normalizedQuery))
                            {
                                matched = true; break;
                            }
                        }
                    }

                    // displayName translations
                    if (!matched && it.Other_DisplayName_Names != null)
                    {
                        foreach (var v in it.Other_DisplayName_Names.Values)
                        {
                            if (!string.IsNullOrWhiteSpace(v) && NormalizeForKey(v).StartsWith(normalizedQuery))
                            {
                                matched = true; break;
                            }
                        }
                    }

                    if (!matched) continue;

                    var key = (it.Settlement ?? "") + "|" + (it.DisplayName ?? "");
                    if (seen.Add(key)) yield return it;
                }
            }

            async Task AddBaseKeyToAliasAsync(string aliasKey, string baseCacheKey)
            {
                // Safe add: get -> modify -> set. For higher throughput replace with Lua or pipeline.
                var existingJson = await _redis.GetAsync(aliasKey);
                List<string> list;
                if (string.IsNullOrWhiteSpace(existingJson))
                    list = new List<string>();
                else
                    list = JsonSerializer.Deserialize<List<string>>(existingJson, _jsonOptions) ?? new List<string>();

                if (list.Contains(baseCacheKey)) return;
                if (list.Count >= MaxAliasListSize)
                {
                    // avoid exploding lists
                    return;
                }

                list.Add(baseCacheKey);
                await _redis.SetAsync(aliasKey, JsonSerializer.Serialize(list, _jsonOptions), AliasTtl);
            }

            async Task<List<string>> BatchGetAsync(IEnumerable<string> keys)
            {
                var results = new List<string>();
                var sem = new SemaphoreSlim(ParallelFetchLimit);
                var tasks = keys.Select(async key =>
                {
                    await sem.WaitAsync();
                    try
                    {
                        return await _redis.GetAsync(key);
                    }
                    finally { sem.Release(); }
                }).ToList();

                var values = await Task.WhenAll(tasks);
                results.AddRange(values.Where(v => !string.IsNullOrWhiteSpace(v)));
                return results;
            }

            // === Validate inputs and setup ===
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
            if (region == null) return Array.Empty<SettlementSuggestionDto>();

            string regionName = region.Name;

            // normalize request
            var normalizedQuery = NormalizeForKey(settlement);
            if (string.IsNullOrWhiteSpace(normalizedQuery)) return Array.Empty<SettlementSuggestionDto>();

            // keys templates
            // --- STABLE cache key part: нормализуем regionName, чтобы ключи были детерминированы ---
            string regionKeyForCache = NormalizeForKey(regionName);
            if (string.IsNullOrWhiteSpace(regionKeyForCache))
            {
                regionKeyForCache = region.Code?.ToUpperInvariant() ?? regionName;
            }

            // keys templates (use normalized region key)
            string aliasPrefix = $"geo:alias:{countryCodeUpper}:{regionKeyForCache}:{limit}"; // aliasPrefix:{normalizedPrefix}
            string baseCacheKeyTemplate = $"geo:settlements:{countryCodeUpper}:{regionKeyForCache}:{limit}"; // append :{normalize(originalQuery)} when saving


            // === 1) Try alias exact match and progressive fallback to shorter prefix ===
            List<string> baseKeys = null;
            string aliasKeyExact = $"{aliasPrefix}:{normalizedQuery}";
            var aliasJson = await _redis.GetAsync(aliasKeyExact);
            if (!string.IsNullOrWhiteSpace(aliasJson))
            {
                baseKeys = JsonSerializer.Deserialize<List<string>>(aliasJson, _jsonOptions) ?? new List<string>();
            }
            else
            {
                // fallback: try progressively shorter prefixes (from full length down to AliasMinPrefixLen)
                for (int len = normalizedQuery.Length - 1; len >= AliasMinPrefixLen && (baseKeys == null || baseKeys.Count == 0); len--)
                {
                    var tryKey = normalizedQuery.Substring(0, len);
                    var ak = $"{aliasPrefix}:{tryKey}";
                    var aj = await _redis.GetAsync(ak);
                    if (!string.IsNullOrWhiteSpace(aj))
                        baseKeys = JsonSerializer.Deserialize<List<string>>(aj, _jsonOptions) ?? new List<string>();
                }
            }

            // === 2) If alias found -> fetch base JSONs, filter, return ===
            if (baseKeys != null && baseKeys.Count > 0)
            {
                var distinctBaseKeys = baseKeys.Distinct().Take(500).ToList(); // cap to avoid huge pulls
                var baseJsons = await BatchGetAsync(distinctBaseKeys);

                var allCandidates = new List<SettlementSuggestionDto>();
                bool hasExplicitEmpty = false;

                foreach (var bj in baseJsons)
                {
                    // явный пустой массив, ранее записанный в кэш -> считаем валидным результатом
                    if (string.Equals(bj?.Trim(), "[]", StringComparison.Ordinal))
                    {
                        hasExplicitEmpty = true;
                        continue;
                    }

                    var arr = JsonSerializer.Deserialize<List<SettlementSuggestionDto>>(bj, _jsonOptions);
                    if (arr != null && arr.Count > 0)
                        allCandidates.AddRange(arr);
                }

                var filtered = FilterAndDedup(allCandidates, normalizedQuery)
                    .Take(limit)
                    .ToList();

                if (filtered.Count > 0)
                    return filtered;

                // Если среди baseJsons был явный "[]" и мы не нашли ни одного кандидата — возвращаем пустой ответ
                if (hasExplicitEmpty && allCandidates.Count == 0)
                    return Array.Empty<SettlementSuggestionDto>();

                // иначе — fallthrough к шагу 3 / Geoapify
            }


            // === 3) If no alias or alias gave no matches: fallback to local base key exact ===
            var normalizedInput = NormalizeForKey(settlement);
            var baseCacheKeyExact = $"{baseCacheKeyTemplate}:{normalizedInput}";
            var cachedBase = await _redis.GetAsync(baseCacheKeyExact);
            if (!string.IsNullOrWhiteSpace(cachedBase))
            {
                // явный кэш пустого результата
                if (string.Equals(cachedBase.Trim(), "[]", StringComparison.Ordinal))
                    return Array.Empty<SettlementSuggestionDto>();

                var baseList = JsonSerializer.Deserialize<List<SettlementSuggestionDto>>(cachedBase, _jsonOptions) ?? new List<SettlementSuggestionDto>();
                var filtered = FilterAndDedup(baseList, normalizedQuery).Take(limit).ToList();
                if (filtered.Count > 0) return filtered;
            }


            // === 4) Last resort: call Geoapify external API ===
            var url = $"/v1/geocode/autocomplete" +
                      $"?text={Uri.EscapeDataString($"{settlement} {regionCode} {countryCode}")}" +
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
                await _redis.SetAsync(baseCacheKeyExact, "[]", TimeSpan.FromHours(1));
                return Array.Empty<SettlementSuggestionDto>();
            }

            if (response?.Features == null || response.Features.Count == 0)
            {
                await _redis.SetAsync(baseCacheKeyExact, "[]", TimeSpan.FromHours(1));
                return Array.Empty<SettlementSuggestionDto>();
            }

            var allowedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "city", "town", "village", "hamlet", "locality", "postcode"
            };

            var rawSuggestions = response.Features
                .Where(f => f.Properties != null)
                .Where(f => f.Properties!.Rank != null && f.Properties!.Rank.ConfidenceCityLevel >= 0.9)
                .Where(f =>
                {
                    var t = f.Properties!.ResultType ?? f.Properties!.Type ?? f.Properties!.PlaceType;
                    return !string.IsNullOrWhiteSpace(t) && allowedTypes.Contains(t);
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

            if (rawSuggestions.Count == 0)
            {
                await _redis.SetAsync(baseCacheKeyExact, "[]", TimeSpan.FromHours(1));
                return Array.Empty<SettlementSuggestionDto>();
            }

            var result = new List<SettlementSuggestionDto>(rawSuggestions.Count);
            foreach (var s in rawSuggestions)
            {
                var displayNameTranslations = await _translateServiceClient.TranslateAsync(s.DisplayName);

                string ExtractSettlementName(string fullName)
                {
                    if (string.IsNullOrWhiteSpace(fullName)) return string.Empty;
                    var parts = fullName.Split(',');
                    return parts[0].Trim();
                }

                var settlementTranslations = displayNameTranslations.ToDictionary(
                    kvp => kvp.Key,
                    kvp => ExtractSettlementName(kvp.Value)
                );

                result.Add(new SettlementSuggestionDto
                {
                    Settlement = s.Settlement,
                    Other_Settlement_Names = settlementTranslations,
                    DisplayName = s.DisplayName,
                    Other_DisplayName_Names = displayNameTranslations
                });
            }

            // === 5) Save base cache and create aliases (limited, parallelized) ===
            var serialized = JsonSerializer.Serialize(result, _jsonOptions);
            await _redis.SetAsync(baseCacheKeyExact, serialized, BaseTtl);

            var aliasTasks = new List<Task>();
            foreach (var item in result)
            {
                // gather names: main settlement + translations + display names
                var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (!string.IsNullOrWhiteSpace(item.Settlement)) names.Add(item.Settlement);
                if (item.Other_Settlement_Names != null)
                {
                    foreach (var v in item.Other_Settlement_Names.Values) if (!string.IsNullOrWhiteSpace(v)) names.Add(v);
                }
                if (!string.IsNullOrWhiteSpace(item.DisplayName)) names.Add(item.DisplayName);
                if (item.Other_DisplayName_Names != null)
                {
                    foreach (var v in item.Other_DisplayName_Names.Values) if (!string.IsNullOrWhiteSpace(v)) names.Add(v);
                }

                foreach (var name in names)
                {
                    int prefixesAdded = 0;
                    foreach (var pref in GenerateLimitedPrefixes(name))
                    {
                        if (prefixesAdded >= MaxPrefixesPerName) break;
                        var aliasKey = $"{aliasPrefix}:{pref}";
                        aliasTasks.Add(AddBaseKeyToAliasAsync(aliasKey, baseCacheKeyExact));
                        prefixesAdded++;
                    }
                }
            }

            // await all alias writes in parallel (non-blocking)
            if (aliasTasks.Count > 0)
                await Task.WhenAll(aliasTasks);

            // final server-side filter and return
            var final = FilterAndDedup(result, normalizedQuery).Take(limit).ToList();
            return final;
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