namespace Classified.Shared.Infrastructure.RedisService
{
    public interface IRedisService
    {
        Task SetAsync(string key, string value, TimeSpan expiration);
        Task<string?> GetAsync(string key);
        Task DeleteAsync(string key);

        Task<IReadOnlyList<string>> GetManyAsync(IEnumerable<string> keys);

        Task AddAliasAsync(string aliasKey, string baseCacheKey, int maxSize, TimeSpan ttl);

        // Новый атомарный метод для уменьшения количества попыток 2FA
        Task<bool> TryDecrementAttemptsAsync(string key, string expectedJsonValue, string newJsonValue, TimeSpan expiration);

        Task<bool> TryDeleteAsync(string key, string expectedJsonValue);
    }
}
