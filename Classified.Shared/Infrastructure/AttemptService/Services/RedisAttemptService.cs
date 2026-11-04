//using Classified.Shared.Infrastructure.AttemptService.Interfaces;
//using Classified.Shared.Infrastructure.AttemptService.Models;
//using StackExchange.Redis;
//using System.Security.Cryptography;
//using System.Text;
//using System.Text.Json;

//namespace Classified.Shared.Infrastructure.AttemptService.Services
//{
//    public sealed class RedisAttemptService : IAttemptService
//    {
//        private readonly IDatabase _database;

//        private const string OperationPrefix = "shared:operations";

//        // =====================================================
//        // INITIALIZE OPERATION
//        // =====================================================

//        private const string InitializeOperationScript = """
//            -- Сохраняем новые данные операции.
//            redis.call('SET', KEYS[1], ARGV[1], 'PX', ARGV[3])

//            -- Сбрасываем количество неудачных попыток.
//            redis.call('SET', KEYS[2], '0', 'PX', ARGV[3])

//            -- Устанавливаем максимальное количество попыток.
//            redis.call('SET', KEYS[3], ARGV[2], 'PX', ARGV[3])

//            return 1
//         """;


//        // =====================================================
//        // VERIFY CODE
//        // =====================================================

//        private const string VerifyCodeScript = """
//            -- Получаем данные операции.
//            local operationJson = redis.call('GET', KEYS[1])

//            if not operationJson then
//                return {0, '', 0, 0}
//            end

//            -- Получаем максимальное количество попыток.
//            local maxAttemptsRaw = redis.call('GET', KEYS[3])

//            -- Получаем количество использованных попыток.
//            local failedAttemptsRaw = redis.call('GET', KEYS[2])

//            -- Если данные счётчиков отсутствуют,
//            -- не разрешаем проверку с обнулением попыток.
//            if not maxAttemptsRaw or not failedAttemptsRaw then
//                return {3, '', 0, 0}
//            end

//            local maxAttempts = tonumber(maxAttemptsRaw)
//            local failedAttempts = tonumber(failedAttemptsRaw)

//            if not maxAttempts
//                or maxAttempts <= 0
//                or not failedAttempts
//                or failedAttempts < 0 then

//                return {3, '', 0, 0}
//            end

//            -- Проверяем, не закончились ли попытки.
//            if failedAttempts >= maxAttempts then
//                return {2, '', failedAttempts, maxAttempts}
//            end

//            -- Десериализуем данные операции.
//            local success, operationData = pcall(cjson.decode, operationJson)

//            if not success or type(operationData) ~= 'table' then
//                return {3, '', failedAttempts, maxAttempts}
//            end

//            -- Получаем код из JSON.
//            local expectedCode = operationData['Code'] or operationData['code']

//            if expectedCode == nil or type(expectedCode) ~= 'string' then
//                return {3, '', failedAttempts, maxAttempts}
//            end

//            -- Сравниваем коды без учёта регистра.
//            if string.lower(expectedCode) ~= string.lower(ARGV[1]) then

//                -- Регистрируем неправильную попытку.
//                local newAttempts = redis.call('INCR', KEYS[2])

//                -- Последняя попытка исчерпана.
//                if newAttempts >= maxAttempts then
//                    return {2, '', newAttempts, maxAttempts}
//                end

//                return {1, '', newAttempts, maxAttempts}
//            end

//            -- Код правильный.
//            -- Атомарно удаляем операцию и все её счётчики.
//            redis.call('DEL', KEYS[1], KEYS[2], KEYS[3])

//            return {4, operationJson, failedAttempts, maxAttempts}
//        """;


//        // =====================================================
//        // CONSTRUCTOR
//        // =====================================================

//        public RedisAttemptService(IConnectionMultiplexer redis)
//        {
//            _database = redis.GetDatabase();
//        }


//        // =====================================================
//        // INITIALIZE OPERATION
//        // =====================================================

//        public async Task InitializeOperationAsync<TData>(
//            string operationKey,
//            string subjectId,
//            TData data,
//            int maxAttempts,
//            TimeSpan expiration,
//            CancellationToken ct)
//            where TData : class, ICodeOperationData
//        {
//            ValidateKeyParameters(operationKey, subjectId);

//            ArgumentNullException.ThrowIfNull(data);

//            if (string.IsNullOrWhiteSpace(data.Code))
//                throw new ArgumentException("Operation code cannot be empty.", nameof(data));

//            if (maxAttempts <= 0)
//                throw new ArgumentOutOfRangeException(nameof(maxAttempts));

//            ValidateExpiration(expiration);

//            ct.ThrowIfCancellationRequested();

//            var keys = BuildKeys(operationKey, subjectId);

//            string serializedData = JsonSerializer.Serialize(data);

//            long expirationMs = GetExpirationMilliseconds(expiration);

//            await _database.ScriptEvaluateAsync(
//                InitializeOperationScript,
//                new RedisKey[]
//                {
//                keys.DataKey,
//                keys.AttemptsKey,
//                keys.MaxAttemptsKey
//                },
//                new RedisValue[]
//                {
//                serializedData,
//                maxAttempts,
//                expirationMs
//                });
//        }


//        // =====================================================
//        // VERIFY CODE
//        // =====================================================

//        public async Task<OperationVerificationResult<TData>> VerifyCodeAsync<TData>(
//            string operationKey,
//            string subjectId,
//            string code,
//            CancellationToken ct)
//            where TData : class, ICodeOperationData
//        {
//            ValidateKeyParameters(operationKey, subjectId);

//            if (string.IsNullOrWhiteSpace(code))
//                throw new ArgumentException("Code cannot be empty.", nameof(code));

//            ct.ThrowIfCancellationRequested();

//            var keys = BuildKeys(operationKey, subjectId);

//            RedisResult result = await _database.ScriptEvaluateAsync(
//                VerifyCodeScript,
//                new RedisKey[]
//                {
//                keys.DataKey,
//                keys.AttemptsKey,
//                keys.MaxAttemptsKey
//                },
//                new RedisValue[]
//                {
//                code
//                });

//            var values = (RedisResult[])result;

//            int statusValue = checked((int)(long)values[0]);

//            if (!Enum.IsDefined(typeof(OperationVerificationStatus), statusValue))
//                throw new InvalidOperationException("Unknown verification status.");

//            var status = (OperationVerificationStatus)statusValue;

//            long failedAttemptsValue = (long)values[2];
//            long maxAttemptsValue = (long)values[3];

//            int failedAttempts = checked((int)Math.Min(
//                failedAttemptsValue,
//                int.MaxValue));

//            int maxAttempts = checked((int)Math.Min(
//                maxAttemptsValue,
//                int.MaxValue));

//            int remainingAttempts = Math.Max(
//                0,
//                maxAttempts - failedAttempts);

//            TData? data = default;

//            // Десериализуем данные только при успешной проверке.
//            if (status == OperationVerificationStatus.Success)
//            {
//                string serializedData = (string)values[1];

//                try
//                {
//                    data = JsonSerializer.Deserialize<TData>(serializedData);
//                }
//                catch (JsonException ex)
//                {
//                    throw new InvalidOperationException(
//                        "Failed to deserialize operation data. Check the TData type.",
//                        ex);
//                }

//                if (data is null)
//                {
//                    throw new InvalidOperationException(
//                        "Operation data deserialization returned null.");
//                }
//            }

//            return new OperationVerificationResult<TData>(
//                Status: status,
//                Data: data,
//                FailedAttempts: failedAttempts,
//                RemainingAttempts: remainingAttempts);
//        }


//        // =====================================================
//        // RESET OPERATION
//        // =====================================================

//        public async Task ResetOperationAsync(
//            string operationKey,
//            string subjectId,
//            CancellationToken ct)
//        {
//            ValidateKeyParameters(operationKey, subjectId);

//            ct.ThrowIfCancellationRequested();

//            var keys = BuildKeys(operationKey, subjectId);

//            await _database.KeyDeleteAsync(
//                new RedisKey[]
//                {
//                keys.DataKey,
//                keys.AttemptsKey,
//                keys.MaxAttemptsKey
//                });
//        }


//        // =====================================================
//        // PRIVATE METHODS
//        // =====================================================

//        private static (
//            RedisKey DataKey,
//            RedisKey AttemptsKey,
//            RedisKey MaxAttemptsKey
//        ) BuildKeys(string operationKey, string subjectId)
//        {
//            // Хеш позволяет гарантировать одинаковый Redis Cluster slot
//            // для всех трёх ключей, даже если subjectId содержит спецсимволы.

//            string rawKey = $"{operationKey}\0{subjectId}";

//            string hashTag = Convert.ToHexString(
//                SHA256.HashData(Encoding.UTF8.GetBytes(rawKey)));

//            string prefix = $"{OperationPrefix}:{operationKey}:{{{hashTag}}}";

//            return (
//                DataKey: $"{prefix}:data",
//                AttemptsKey: $"{prefix}:attempts",
//                MaxAttemptsKey: $"{prefix}:max-attempts"
//            );
//        }

//        private static long GetExpirationMilliseconds(TimeSpan expiration)
//        {
//            return checked((long)Math.Ceiling(expiration.TotalMilliseconds));
//        }

//        private static void ValidateKeyParameters(
//            string operationKey,
//            string subjectId)
//        {
//            if (string.IsNullOrWhiteSpace(operationKey))
//                throw new ArgumentException(
//                    "Operation key cannot be empty.",
//                    nameof(operationKey));

//            if (string.IsNullOrWhiteSpace(subjectId))
//                throw new ArgumentException(
//                    "Subject ID cannot be empty.",
//                    nameof(subjectId));
//        }

//        private static void ValidateExpiration(TimeSpan expiration)
//        {
//            if (expiration <= TimeSpan.Zero)
//                throw new ArgumentOutOfRangeException(
//                    nameof(expiration),
//                    "Expiration must be greater than zero.");
//        }
//    }
//}

using Classified.Shared.Infrastructure.AttemptService.Interfaces;
using Classified.Shared.Infrastructure.AttemptService.Models;
using Classified.Shared.Libs.CodeHasher;
using StackExchange.Redis;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Classified.Shared.Infrastructure.AttemptService.Services
{
    public sealed class RedisAttemptService : IAttemptService
    {
        private readonly IDatabase _database;
        private readonly ICodeHasher _codeHasher;

        private const string OperationPrefix = "shared:operations";

        // Внутренний статус: операция изменилась между чтением
        // данных и выполнением Lua-скрипта.
        private const int ConcurrentChangeStatus = 5;
        private const int MaxConcurrencyRetries = 5;

        // =====================================================
        // INITIALIZE OPERATION
        // =====================================================

        private const string InitializeOperationScript = """
            -- Сохраняем данные операции.
            redis.call('SET', KEYS[1], ARGV[1], 'PX', ARGV[3])

            -- Сбрасываем количество неудачных попыток.
            redis.call('SET', KEYS[2], '0', 'PX', ARGV[3])

            -- Устанавливаем максимальное количество попыток.
            redis.call('SET', KEYS[3], ARGV[2], 'PX', ARGV[3])

            return 1
            """;

        // =====================================================
        // VERIFY CODE
        // =====================================================

        private const string VerifyCodeScript = """
            -- Получаем данные операции.
            local operationJson = redis.call('GET', KEYS[1])

            if not operationJson then
                return {0, '', 0, 0}
            end

            -- Проверяем, что операция не изменилась
            -- с момента чтения данных в C#.
            if operationJson ~= ARGV[1] then
                return {5, '', 0, 0}
            end

            -- Получаем максимальное количество попыток.
            local maxAttemptsRaw = redis.call('GET', KEYS[3])

            -- Получаем количество использованных попыток.
            local failedAttemptsRaw = redis.call('GET', KEYS[2])

            -- Проверяем наличие счётчиков.
            if not maxAttemptsRaw or not failedAttemptsRaw then
                return {3, '', 0, 0}
            end

            local maxAttempts = tonumber(maxAttemptsRaw)
            local failedAttempts = tonumber(failedAttemptsRaw)

            if not maxAttempts
                or maxAttempts <= 0
                or not failedAttempts
                or failedAttempts < 0 then

                return {3, '', 0, 0}
            end

            -- Проверяем, не закончились ли попытки.
            if failedAttempts >= maxAttempts then
                return {2, '', failedAttempts, maxAttempts}
            end

            -- Проверяем корректность данных операции.
            -- ARGV[3] = 0 означает, что C# не смог
            -- корректно десериализовать сохранённые данные.
            if ARGV[3] ~= '1' then
                return {3, '', failedAttempts, maxAttempts}
            end

            -- ARGV[2] = 1 означает, что HMAC-проверка успешна.
            if ARGV[2] == '1' then

                -- Код правильный.
                -- Атомарно удаляем операцию и счётчики.
                redis.call('DEL', KEYS[1], KEYS[2], KEYS[3])

                return {4, operationJson, failedAttempts, maxAttempts}
            end

            -- Код неправильный.
            local newAttempts = redis.call('INCR', KEYS[2])

            -- Последняя попытка исчерпана.
            if newAttempts >= maxAttempts then
                return {2, '', newAttempts, maxAttempts}
            end

            return {1, '', newAttempts, maxAttempts}
            """;

        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public RedisAttemptService(
            IConnectionMultiplexer redis,
            ICodeHasher codeHasher)
        {
            ArgumentNullException.ThrowIfNull(redis);
            ArgumentNullException.ThrowIfNull(codeHasher);

            _database = redis.GetDatabase();
            _codeHasher = codeHasher;
        }

        // =====================================================
        // INITIALIZE OPERATION
        // =====================================================

        public async Task InitializeOperationAsync<TData>(
            string operationKey,
            string subjectId,
            TData data,
            int maxAttempts,
            TimeSpan expiration,
            CancellationToken ct)
            where TData : class, ICodeOperationData
        {
            ValidateKeyParameters(operationKey, subjectId);

            ArgumentNullException.ThrowIfNull(data);

            if (string.IsNullOrWhiteSpace(data.Code))
                throw new ArgumentException(
                    "Operation code cannot be empty.",
                    nameof(data));

            if (maxAttempts <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxAttempts));

            ValidateExpiration(expiration);

            ct.ThrowIfCancellationRequested();

            var keys = BuildKeys(operationKey, subjectId);

            // Хешируем исходный код.
            string codeHash = _codeHasher.Hash(data.Code);

            // Сериализуем DTO, заменяя обычный код его хешем.
            string serializedData = SerializeWithHashedCode(data, codeHash);

            long expirationMs = GetExpirationMilliseconds(expiration);

            await _database.ScriptEvaluateAsync(
                InitializeOperationScript,
                new RedisKey[]
                {
                    keys.DataKey,
                    keys.AttemptsKey,
                    keys.MaxAttemptsKey
                },
                new RedisValue[]
                {
                    serializedData,
                    maxAttempts,
                    expirationMs
                });
        }

        // =====================================================
        // VERIFY CODE
        // =====================================================

        public async Task<OperationVerificationResult<TData>> VerifyCodeAsync<TData>(
            string operationKey,
            string subjectId,
            string code,
            CancellationToken ct)
            where TData : class, ICodeOperationData
        {
            ValidateKeyParameters(operationKey, subjectId);

            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException(
                    "Code cannot be empty.",
                    nameof(code));

            var keys = BuildKeys(operationKey, subjectId);

            for (int retry = 0; retry < MaxConcurrencyRetries; retry++)
            {
                ct.ThrowIfCancellationRequested();

                // Получаем сохранённые данные операции.
                RedisValue storedValue = await _database.StringGetAsync(
                    keys.DataKey);

                string operationJson = storedValue.IsNull
                    ? string.Empty
                    : storedValue.ToString();

                TData? storedData = null;
                bool isDataValid = false;
                bool isCodeValid = false;

                // Если операция существует, читаем DTO и проверяем HMAC.
                if (!storedValue.IsNull)
                {
                    try
                    {
                        storedData = JsonSerializer.Deserialize<TData>(
                            operationJson);

                        if (storedData is not null &&
                            !string.IsNullOrWhiteSpace(storedData.Code))
                        {
                            isDataValid = true;

                            isCodeValid = _codeHasher.Verify(
                                code,
                                storedData.Code);
                        }
                    }
                    catch (JsonException)
                    {
                        isDataValid = false;
                    }
                    catch (NotSupportedException)
                    {
                        isDataValid = false;
                    }
                }

                ct.ThrowIfCancellationRequested();

                // Lua получает только JSON с хешем и результат проверки.
                // Сам введённый код в Redis не передаётся.
                RedisResult result = await _database.ScriptEvaluateAsync(
                    VerifyCodeScript,
                    new RedisKey[]
                    {
                        keys.DataKey,
                        keys.AttemptsKey,
                        keys.MaxAttemptsKey
                    },
                    new RedisValue[]
                    {
                        operationJson,
                        isCodeValid ? "1" : "0",
                        isDataValid ? "1" : "0"
                    });

                var values = (RedisResult[])result;

                int statusValue = checked((int)(long)values[0]);

                // Операция изменилась между чтением и Lua-проверкой.
                // Повторяем чтение и проверку уже актуального кода.
                if (statusValue == ConcurrentChangeStatus)
                    continue;

                if (!Enum.IsDefined(
                    typeof(OperationVerificationStatus),
                    statusValue))
                {
                    throw new InvalidOperationException(
                        "Unknown verification status.");
                }

                var status = (OperationVerificationStatus)statusValue;

                long failedAttemptsValue = (long)values[2];
                long maxAttemptsValue = (long)values[3];

                int failedAttempts = checked((int)Math.Min(
                    failedAttemptsValue,
                    int.MaxValue));

                int maxAttempts = checked((int)Math.Min(
                    maxAttemptsValue,
                    int.MaxValue));

                int remainingAttempts = Math.Max(
                    0,
                    maxAttempts - failedAttempts);

                TData? data = default;

                // При успешной проверке восстанавливаем DTO.
                // В Data.Code будет введённый код, а не хеш.
                if (status == OperationVerificationStatus.Success)
                {
                    string serializedData = (string)values[1];

                    try
                    {
                        data = DeserializeWithOriginalCode<TData>(
                            serializedData,
                            code);
                    }
                    catch (JsonException ex)
                    {
                        throw new InvalidOperationException(
                            "Failed to deserialize operation data.",
                            ex);
                    }
                }

                return new OperationVerificationResult<TData>(
                    Status: status,
                    Data: data,
                    FailedAttempts: failedAttempts,
                    RemainingAttempts: remainingAttempts);
            }

            throw new InvalidOperationException(
                "Operation was repeatedly modified during code verification.");
        }

        // =====================================================
        // RESET OPERATION
        // =====================================================

        public async Task ResetOperationAsync(
            string operationKey,
            string subjectId,
            CancellationToken ct)
        {
            ValidateKeyParameters(operationKey, subjectId);

            ct.ThrowIfCancellationRequested();

            var keys = BuildKeys(operationKey, subjectId);

            await _database.KeyDeleteAsync(
                new RedisKey[]
                {
                    keys.DataKey,
                    keys.AttemptsKey,
                    keys.MaxAttemptsKey
                });
        }

        // =====================================================
        // SERIALIZATION
        // =====================================================

        private static string SerializeWithHashedCode<TData>(
            TData data,
            string codeHash)
            where TData : ICodeOperationData
        {
            var jsonObject = JsonSerializer.SerializeToNode(data)
                as JsonObject
                ?? throw new InvalidOperationException(
                    "Operation data must be a JSON object.");

            string codePropertyName = GetCodePropertyName(jsonObject);

            jsonObject[codePropertyName] = codeHash;

            return jsonObject.ToJsonString();
        }

        private static TData DeserializeWithOriginalCode<TData>(
            string serializedData,
            string originalCode)
            where TData : ICodeOperationData
        {
            var jsonObject = JsonNode.Parse(serializedData)
                as JsonObject
                ?? throw new JsonException(
                    "Operation data must be a JSON object.");

            string codePropertyName = GetCodePropertyName(jsonObject);

            // Не возвращаем вызывающему коду сохранённый HMAC.
            jsonObject[codePropertyName] = originalCode;

            return jsonObject.Deserialize<TData>()
                ?? throw new JsonException(
                    "Operation data deserialization returned null.");
        }

        private static string GetCodePropertyName(JsonObject jsonObject)
        {
            foreach (var property in jsonObject)
            {
                if (string.Equals(
                    property.Key,
                    nameof(ICodeOperationData.Code),
                    StringComparison.OrdinalIgnoreCase))
                {
                    return property.Key;
                }
            }

            throw new JsonException(
                "Code property was not found in operation data.");
        }

        // =====================================================
        // PRIVATE METHODS
        // =====================================================

        private static (
            RedisKey DataKey,
            RedisKey AttemptsKey,
            RedisKey MaxAttemptsKey
        ) BuildKeys(string operationKey, string subjectId)
        {
            // Все три ключа гарантированно попадают
            // в один Redis Cluster slot.

            string rawKey = $"{operationKey}\0{subjectId}";

            string hashTag = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(rawKey)));

            string prefix =
                $"{OperationPrefix}:{operationKey}:{{{hashTag}}}";

            return (
                DataKey: $"{prefix}:data",
                AttemptsKey: $"{prefix}:attempts",
                MaxAttemptsKey: $"{prefix}:max-attempts"
            );
        }

        private static long GetExpirationMilliseconds(TimeSpan expiration)
        {
            return checked((long)Math.Ceiling(expiration.TotalMilliseconds));
        }

        private static void ValidateKeyParameters(
            string operationKey,
            string subjectId)
        {
            if (string.IsNullOrWhiteSpace(operationKey))
                throw new ArgumentException(
                    "Operation key cannot be empty.",
                    nameof(operationKey));

            if (string.IsNullOrWhiteSpace(subjectId))
                throw new ArgumentException(
                    "Subject ID cannot be empty.",
                    nameof(subjectId));
        }

        private static void ValidateExpiration(TimeSpan expiration)
        {
            if (expiration <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(
                    nameof(expiration),
                    "Expiration must be greater than zero.");
        }
    }
}