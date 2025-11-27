using Classified.Shared.DTOs;
using GeoService.Domain.Models;

namespace GeoService.Domain.Abstractions
{
    public interface IMeteoGeoService
    {

        Task<IReadOnlyList<PlaceSuggestion>> GetMeteoGeoServiceSettlementSuggestionsAsync(
            string query,
            string? countryCode = null,
            string? regionCode = null,
            int limit = 10);
    }

    public interface IOpenCageGeoService
    {

        Task<IReadOnlyList<PlaceSuggestion>> GetSettlementSuggestionsAsync(
            string query, 
            string? countryCode = null,
            string? stateCode = null,
            int limit = 10);

        Task<IReadOnlyList<string>> GetPostcodeSuggestionsAsync(
            string countryCode,
            string? stateCode,
            string city);

    }

    public interface IGeoapifyGeoService
    {
        Task<IReadOnlyList<SettlementSuggestionDto>> GetSettlementSuggestionsAsync(
            string countryCode,
            string regionCode,
            string settlement
        );

        Task<IReadOnlyList<PlaceSuggestion>> GetAddressSuggestionsAsync(
            string countryCode,
            string stateCode,
            string settlement,
            string streetAndNumber
        );

        Task<IReadOnlyList<string>> GetPostcodesAsync(
            string countryCode,
            string regionCode,
            string settlement,
            string streetAndNumber
        );

        //Task<GeoapifyResult?> GetValidatedFullAddress(
        //   string countryCode,
        //   string regionCode,
        //   string settlement,
        //   string street,
        //   int zipCode
        //);

        Task<GeoapifyResult?> GetValidatedFullAddress(
            string text
        );


        Task<GeoapifyResult?> GetValidatedSettlement(
            string countryCode,
            string regionCode,
            string settlement
        );
    }

}
