using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;

namespace UserService.Persistance.PostgreSQL.Services
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
                        .GetRequiredService<UserServicePostgreDbContext>();

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
            UserServicePostgreDbContext context,
            CancellationToken token)
        {
            var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);

            var affected = await context.Users
                .Where(u => u.IsSoftDeleted &&
                            !u.IsPermanantlyDeleted &&
                            u.DeletedAt <= sixMonthsAgo)
                .ExecuteUpdateAsync(u =>
                    u.SetProperty(x => x.IsPermanantlyDeleted, true),
                    cancellationToken: token);

            if (affected > 0)
            {
                Console.WriteLine($"{DateTime.UtcNow}: PermanentDelete updated {affected} users.");
            }
        }

    }
}