using StackExchange.Redis;

namespace Classified.Shared.Infrastructure.RedisService
{
    public class RedisService : IRedisService
    {
        private readonly IDatabase _database;

        public RedisService(IConnectionMultiplexer connectionMultiplexer)
        {
            _database = connectionMultiplexer.GetDatabase();
        }

        public async Task SetAsync(string key, string value, TimeSpan expiration)
        {
            await _database.StringSetAsync(key, value, expiration);
        }

        public async Task<string?> GetAsync(string key)
        {
            return await _database.StringGetAsync(key);
        }

        public async Task DeleteAsync(string key)
        {
            await _database.KeyDeleteAsync(key);
        }

        // Внутри реализации RedisService (где есть доступ к StackExchange.Redis):
        public async Task<bool> TryDeleteAsync(string key, string expectedJsonValue)
        {
            var transaction = _database.CreateTransaction();

            // Удаляем только если значение не изменилось с момента проверки
            transaction.AddCondition(Condition.StringEqual(key, expectedJsonValue));
            _ = transaction.KeyDeleteAsync(key);

            return await transaction.ExecuteAsync();
        }


        public async Task<IReadOnlyList<string>> GetManyAsync(IEnumerable<string> keys)
        {
            var redisKeys = keys
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .Select(k => (RedisKey)k)
                .ToArray();

            if (redisKeys.Length == 0)
                return Array.Empty<string>();

            RedisValue[] values = await _database.StringGetAsync(redisKeys);

            var result = new List<string>(values.Length);
            foreach (var v in values)
            {
                if (v.HasValue)
                    result.Add(v.ToString());
            }

            return result;
        }

        public async Task AddAliasAsync(
            string aliasKey,
            string baseCacheKey,
            int maxSize,
            TimeSpan ttl
        )
        {
            await _database.ScriptEvaluateAsync(
                AddToAliasLua,
                new RedisKey[] { aliasKey },
                new RedisValue[]
                {
                    baseCacheKey,
                    maxSize,
                    (int)ttl.TotalSeconds
                });
        }

        public async Task<bool> TryDecrementAttemptsAsync(string key, string expectedJsonValue, string newJsonValue, TimeSpan expiration)
        {
            // Создаем транзакцию Redis
            var transaction = _database.CreateTransaction();

            // Главное условие (аналог SQL WHERE): 
            // Выполнить транзакцию ТОЛЬКО если значение в Redis совпадает с тем, что мы прочитали
            transaction.AddCondition(Condition.StringEqual(key, expectedJsonValue));

            // Выстраиваем команду на запись в очередь транзакции
            _ = transaction.StringSetAsync(key, newJsonValue, expiration);

            // Выполняем. Если между чтением и записью другой поток изменил данные, ExecuteAsync вернет false
            return await transaction.ExecuteAsync();
        }


        private const string AddToAliasLua = @"
            -- KEYS[1] = alias key
            -- ARGV[1] = base cache key
            -- ARGV[2] = max list size
            -- ARGV[3] = ttl in seconds

            local current = redis.call('GET', KEYS[1])

            if not current then
                redis.call(
                    'SET',
                    KEYS[1],
                    cjson.encode({ARGV[1]}),
                    'EX',
                    ARGV[3]
                )
                return 1
            end

            local arr = cjson.decode(current)

            for _, v in ipairs(arr) do
                if v == ARGV[1] then
                    return 0
                end
            end

            if #arr < tonumber(ARGV[2]) then
                table.insert(arr, ARGV[1])
                redis.call(
                    'SET',
                    KEYS[1],
                    cjson.encode(arr),
                    'EX',
                    ARGV[3]
                )
                return 1
            end

            return 0
        ";

    }
}
