using AuthService.Persistance.Configurations;
using AuthService.Persistance.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Persistance
{
    public class AuthServicePostgreDbContext(DbContextOptions<AuthServicePostgreDbContext> options) : DbContext(options)
    {
        public DbSet<SessionEntity> Sessions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SessionEntity>()
              .HasIndex(rt => new { rt.UserId, rt.DeviceId });

            modelBuilder.ApplyConfiguration(new SessionConfiguration());

            base.OnModelCreating(modelBuilder);
        }
    }
}
