using Classified.Shared.Extensions.RateLimiting.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace Classified.Shared.Extensions.RateLimiting.Providers
{
    //public sealed class MemoryRateLimitProvider : IMemoryRateLimitProvider
    //{
    //    private readonly IMemoryCache _cache;
    //    private readonly object _creationLock = new();

    //    public MemoryRateLimitProvider(IMemoryCache cache)
    //    {
    //        _cache = cache;
    //    }

    //    public RateLimitResult Consume(
    //        IReadOnlyCollection<string> keys,
    //        int permitLimit,
    //        int windowSeconds)
    //    {
    //        var counters = keys
    //            .OrderBy(x => x, StringComparer.Ordinal)
    //            .Select(GetOrCreateCounter)
    //            .ToArray();

    //        // Все ключи блокируются в одинаковом порядке.
    //        // Это предотвращает взаимную блокировку потоков.
    //        return ConsumeLocked(counters, permitLimit);
    //    }

    //    private Counter GetOrCreateCounter(string key)
    //    {
    //        lock (_creationLock)
    //        {
    //            if (_cache.TryGetValue<Counter>(key, out var existing)
    //                && existing is not null)
    //            {
    //                return existing;
    //            }

    //            var counter = new Counter();

    //            _cache.Set(
    //                key,
    //                counter,
    //                new MemoryCacheEntryOptions
    //                {
    //                    AbsoluteExpirationRelativeToNow =
    //                        TimeSpan.FromSeconds(1), // заменяется ниже
    //                    Size = 1
    //                });

    //            return counter;
    //        }
    //    }

    //    private static RateLimitResult ConsumeLocked(
    //        Counter[] counters,
    //        int permitLimit)
    //    {
    //        return LockCounters(counters, 0, () =>
    //        {
    //            foreach (var counter in counters)
    //            {
    //                if (counter.Count >= permitLimit)
    //                {
    //                    return new RateLimitResult(
    //                        false,
    //                        Math.Max(1, (long)Math.Ceiling(
    //                            (counter.ExpiresAt - DateTime.UtcNow)
    //                            .TotalSeconds)));
    //                }
    //            }

    //            foreach (var counter in counters)
    //                counter.Count++;

    //            return new RateLimitResult(true);
    //        });
    //    }

    //    private static RateLimitResult LockCounters(
    //        Counter[] counters,
    //        int index,
    //        Func<RateLimitResult> action)
    //    {
    //        if (index == counters.Length)
    //            return action();

    //        lock (counters[index].Sync)
    //        {
    //            return LockCounters(counters, index + 1, action);
    //        }
    //    }

    //    private sealed class Counter
    //    {
    //        public object Sync { get; } = new();

    //        public int Count { get; set; }

    //        public DateTime ExpiresAt { get; } =
    //            DateTime.UtcNow.AddSeconds(60);
    //    }
    //}

    //public sealed class MemoryRateLimitProvider : IMemoryRateLimitProvider
    //{
    //    private readonly ConcurrentDictionary<string, Counter> _counters = new();

    //    public RateLimitResult Consume(
    //        IReadOnlyCollection<string> keys,
    //        int permitLimit,
    //        int windowSeconds)
    //    {
    //        if (permitLimit <= 0)
    //            throw new ArgumentOutOfRangeException(nameof(permitLimit));

    //        if (windowSeconds <= 0)
    //            throw new ArgumentOutOfRangeException(nameof(windowSeconds));

    //        var counters = keys
    //            .Distinct(StringComparer.Ordinal)
    //            .OrderBy(x => x, StringComparer.Ordinal)
    //            .Select(key => _counters.GetOrAdd(key, _ => new Counter()))
    //            .ToArray();

    //        if (counters.Length == 0)
    //            throw new ArgumentException("At least one key is required.");

    //        return LockCounters(counters, 0, () =>
    //        {
    //            var now = DateTime.UtcNow;

    //            foreach (var counter in counters)
    //            {
    //                if (counter.ExpiresAt <= now)
    //                {
    //                    counter.Count = 0;
    //                    counter.ExpiresAt = now.AddSeconds(windowSeconds);
    //                }
    //            }

    //            foreach (var counter in counters)
    //            {
    //                if (counter.Count >= permitLimit)
    //                {
    //                    var retryAfter = Math.Max(
    //                        1,
    //                        (long)Math.Ceiling(
    //                            (counter.ExpiresAt - now).TotalSeconds));

    //                    return new RateLimitResult(false, retryAfter);
    //                }
    //            }

    //            foreach (var counter in counters)
    //                counter.Count++;

    //            return new RateLimitResult(true);
    //        });
    //    }

    //    public int CleanupExpired()
    //    {
    //        var now = DateTime.UtcNow;
    //        var removed = 0;

    //        foreach (var entry in _counters)
    //        {
    //            var counter = entry.Value;

    //            lock (counter.Sync)
    //            {
    //                if (counter.ExpiresAt <= now &&
    //                    _counters.TryRemove(
    //                        new KeyValuePair<string, Counter>(
    //                            entry.Key, counter)))
    //                {
    //                    removed++;
    //                }
    //            }
    //        }

    //        return removed;
    //    }

    //    private static RateLimitResult LockCounters(
    //        Counter[] counters,
    //        int index,
    //        Func<RateLimitResult> action)
    //    {
    //        if (index == counters.Length)
    //            return action();

    //        lock (counters[index].Sync)
    //        {
    //            return LockCounters(counters, index + 1, action);
    //        }
    //    }

    //    private sealed class Counter
    //    {
    //        public object Sync { get; } = new();

    //        public int Count { get; set; }

    //        public DateTime ExpiresAt { get; set; } =
    //            DateTime.UtcNow;
    //    }
    //}




    public sealed class MemoryRateLimitProvider : IMemoryRateLimitProvider
    {
        private const int MaxEntries = 500_000;

        private readonly ConcurrentDictionary<string, Counter> _counters = new();

        // Защищает создание и удаление счётчиков.
        private readonly object _gate = new();

        public RateLimitResult Consume(
            IReadOnlyCollection<string> keys,
            int permitLimit,
            int windowSeconds)
        {
            if (permitLimit <= 0)
                throw new ArgumentOutOfRangeException(nameof(permitLimit));

            if (windowSeconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(windowSeconds));

            var uniqueKeys = keys
                .Distinct(StringComparer.Ordinal)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray();

            if (uniqueKeys.Length == 0)
                throw new ArgumentException("At least one key is required.");

            Counter[] counters;

            lock (_gate)
            {
                var missingCount = uniqueKeys.Count(
                    key => !_counters.ContainsKey(key));

                // Если не хватает места, сначала очищаем просроченные записи.
                if (_counters.Count + missingCount > MaxEntries)
                {
                    CleanupExpiredInternal();

                    missingCount = uniqueKeys.Count(
                        key => !_counters.ContainsKey(key));

                    if (_counters.Count + missingCount > MaxEntries)
                    {
                        return new RateLimitResult(
                            Allowed: false,
                            CapacityExceeded: true);
                    }
                }

                counters = uniqueKeys
                    .Select(key => _counters.GetOrAdd(
                        key, _ => new Counter()))
                    .ToArray();

                // Захватываем блокировки до освобождения _gate.
                // Это не позволяет очистке удалить счётчик,
                // который уже получил другой запрос.
                foreach (var counter in counters)
                    Monitor.Enter(counter.Sync);
            }

            try
            {
                var now = DateTime.UtcNow;

                foreach (var counter in counters)
                {
                    if (counter.ExpiresAt <= now)
                    {
                        counter.Count = 0;
                        counter.ExpiresAt = now.AddSeconds(windowSeconds);
                    }
                }

                foreach (var counter in counters)
                {
                    if (counter.Count >= permitLimit)
                    {
                        var retryAfter = Math.Max(
                            1,
                            (long)Math.Ceiling(
                                (counter.ExpiresAt - now).TotalSeconds));

                        return new RateLimitResult(
                            Allowed: false,
                            RetryAfter: retryAfter);
                    }
                }

                foreach (var counter in counters)
                    counter.Count++;

                return new RateLimitResult(Allowed: true);
            }
            finally
            {
                for (var i = counters.Length - 1; i >= 0; i--)
                    Monitor.Exit(counters[i].Sync);
            }
        }

        public int CleanupExpired()
        {
            lock (_gate)
            {
                return CleanupExpiredInternal();
            }
        }

        private int CleanupExpiredInternal()
        {
            var now = DateTime.UtcNow;
            var removed = 0;

            foreach (var entry in _counters)
            {
                var counter = entry.Value;

                lock (counter.Sync)
                {
                    if (counter.ExpiresAt <= now &&
                        _counters.TryRemove(
                            new KeyValuePair<string, Counter>(
                                entry.Key,
                                counter)))
                    {
                        removed++;
                    }
                }
            }

            return removed;
        }

        private sealed class Counter
        {
            public object Sync { get; } = new();

            public int Count { get; set; }

            public DateTime ExpiresAt { get; set; } =
                DateTime.UtcNow;
        }
    }

}
