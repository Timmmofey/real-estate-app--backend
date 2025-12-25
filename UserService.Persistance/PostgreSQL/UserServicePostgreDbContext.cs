using Microsoft.EntityFrameworkCore;
using UserService.Persistance.PostgreSQL.Configurations;
using UserService.Persistance.PostgreSQL.Entities;

namespace UserService.Persistance.PostgreSQL
{
    public class UserServicePostgreDbContext : DbContext
    {
        public UserServicePostgreDbContext(DbContextOptions<UserServicePostgreDbContext> options)
            : base(options)
        {
        }

        public DbSet<UserEntity> Users { get; set; }
        public DbSet<PersonProfileEntity> PersonProfiles { get; set; }
        public DbSet<CompanyProfileEntity> CompanyProfiles { get; set; }
        public DbSet<UserOAuthAccountEntity> UserOAuthAccounts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new PersonPeofileConfiguration());
            modelBuilder.ApplyConfiguration(new CompanyProfileConfiguration());
            modelBuilder.ApplyConfiguration(new UserOAuthAccountConfiguration());

            base.OnModelCreating(modelBuilder);
        }
    }


}
