using Classified.Shared.Infrastructure.S3.Abstractions;
using Classified.Shared.Infrastructure.S3.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Classified.Shared.Infrastructure.EmailService
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddEmailService(this IServiceCollection services, IConfiguration configuration)
        {

            services.AddSingleton<IEmailService, EmailService>();
            return services;

        }
    }
}