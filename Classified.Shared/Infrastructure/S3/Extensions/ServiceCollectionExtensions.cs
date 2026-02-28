using Classified.Shared.Infrastructure.S3.Abstractions;
using Classified.Shared.Infrastructure.S3.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Classified.Shared.Infrastructure.S3.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSufyS3Storage(this IServiceCollection services)
        {
   
            services.AddSingleton<IFileStorageService, SufyStorageService>();
            return services;

        }
    }
}
