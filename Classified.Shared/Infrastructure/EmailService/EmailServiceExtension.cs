using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Classified.Shared.Infrastructure.EmailService
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddEmailService(this IServiceCollection services)
        {

            services.AddSingleton<IEmailService, EmailService>();
            return services;

        }
    }
}