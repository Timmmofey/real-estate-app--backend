using UserService.Application.Abstactions;
using UserService.Application.Services;

namespace UserService.API.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IUserService, UserService.Application.Services.UserService>();
            services.AddScoped<IUserOAuthAccountSevice, UserOAuthAccountSevice>();

            return services;
        }
    }
}
