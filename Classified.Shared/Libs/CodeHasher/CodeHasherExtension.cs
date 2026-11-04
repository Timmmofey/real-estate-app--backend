using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Classified.Shared.Libs.CodeHasher
{
    public static class CodeHasherExtension
    {
        public static IServiceCollection AddCodesHasher(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<CodeHasherOptions>(
                configuration.GetSection(CodeHasherOptions.SectionName));

            services.AddSingleton<ICodeHasher, HmacCodeHasher>();

            return services;
        }
    }
}
