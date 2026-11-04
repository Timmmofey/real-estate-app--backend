using Classified.Shared.Infrastructure.AttemptService.Interfaces;
using Classified.Shared.Infrastructure.AttemptService.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Classified.Shared.Infrastructure.AttemptService.Extensions
{
    public static class AttemptServiceExtension
    {
        public static IServiceCollection AddSharedRedisAttempts(this IServiceCollection services)
        {
            services.AddSingleton<IAttemptService, RedisAttemptService>();

            return services;
        }
    }
}
