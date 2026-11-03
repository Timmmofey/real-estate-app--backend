using Classified.Shared.Extensions.RateLimiting.Providers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Classified.Shared.Extensions.RateLimiting.Services
{
    public sealed class MemoryRateLimitCleanupService : BackgroundService
    {
        private readonly IMemoryRateLimitProvider _provider;
        private readonly ILogger<MemoryRateLimitCleanupService> _logger;

        public MemoryRateLimitCleanupService(
            IMemoryRateLimitProvider provider,
            ILogger<MemoryRateLimitCleanupService> logger)
        {
            _provider = provider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(
                TimeSpan.FromSeconds(30));

            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    var removed = _provider.CleanupExpired();

                    if (removed > 0)
                    {
                        _logger.LogInformation(
                            "Rate limiter cleaned {Count} expired counters",
                            removed);
                    }
                }
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                // Нормальное завершение приложения.
            }
        }
    }
}
