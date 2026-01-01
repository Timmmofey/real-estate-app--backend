namespace Classified.Shared.Infrastructure.RedisService
{
    public interface IRedisService
    {
        Task SetAsync(string key, string value, TimeSpan expiration);
        Task<string?> GetAsync(string key);
        Task DeleteAsync(string key);

        Task<IReadOnlyList<string>> GetManyAsync(IEnumerable<string> keys);

        Task AddAliasAsync(string aliasKey, string baseCacheKey, int maxSize, TimeSpan ttl);
    }
}
