using UserService.Domain.Abstactions;
using UserService.Persistance.PostgreSQL;
using UserService.Persistance.PostgreSQL.Repositories;

namespace UserService.API.Extensions
{
    public static class RepositoryCollectionExtensions
    {
        public static IServiceCollection AddApplicationRepositories(this IServiceCollection services)
        {
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUserOAuthAccountRepository, UserOAuthAccountRepository>();
            services.AddScoped<IUnitOfWork, EfUnitOfWork>();

            return services;
        }

    }
}
