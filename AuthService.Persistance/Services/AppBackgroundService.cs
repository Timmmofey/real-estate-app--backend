using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AuthService.Persistance.PostgreSQL.Services
{
    public class AppBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public AppBackgroundService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var context = scope.ServiceProvider
                        .GetRequiredService<AuthServicePostgreDbContext>();

                    await HandlePermanentDeletion(context, stoppingToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AppBackgroundService] Error: {ex}");
                }

                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }

        private static async Task HandlePermanentDeletion(
            AuthServicePostgreDbContext context,
            CancellationToken token)
        {
            var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);

            var affected = await context.Sessions
                .Where(s => s.ExpiresAt <= DateTime.Now)
                .ExecuteDeleteAsync();

            if (affected > 0)
            {
                Console.WriteLine($"{DateTime.UtcNow}: Deleted sessions: {affected}.");
            }
        }

    }
}