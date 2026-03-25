using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserService.Persistance.PostgreSQL.Entities;

namespace UserService.Persistance.PostgreSQL.Configurations
{
    public class UserOAuthAccountConfiguration: IEntityTypeConfiguration<UserOAuthAccountEntity>
    {
        public void Configure(EntityTypeBuilder<UserOAuthAccountEntity> builder)
        {
            builder.HasKey(a => a.Id);

            builder.HasOne(a => a.User)
                .WithMany(a => a.UserOAuthAccounts);

            builder.Property(u => u.OAuthProviderName)
                  .HasConversion<string>();

            builder
                .HasIndex(x => new { x.UserId, x.OAuthProviderName })
                .IsUnique();

            builder.HasIndex(x => new { x.OAuthProviderName, x.ProviderUserId })
                .IsUnique();
        }

    }
}
