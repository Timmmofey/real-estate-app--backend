namespace UserService.Infrastructure.GeoService
{
    public interface IGeoServiceClient
    {
        Task<bool> ValidateSettlement(string countryCode, string regionCode, string settlement);
    }
}
