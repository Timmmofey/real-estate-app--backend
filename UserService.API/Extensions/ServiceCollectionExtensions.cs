using UserService.Application.Abstactions;

namespace UserService.API.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IUserService, UserService.Application.Services.UserService>();
            return services;
        }
    }
}
