using UserService.Domain.Abstactions;
using UserService.Persistance.PostgreSQL.Repositories;

namespace UserService.API.Extensions
{
    public static class RepositoryCollectionExtensions
    {
        public static IServiceCollection AddApplicationRepositories(this IServiceCollection services)
        {
            services.AddScoped<IUserRepository, UserRepository>();
            return services;
        }
    }
}
