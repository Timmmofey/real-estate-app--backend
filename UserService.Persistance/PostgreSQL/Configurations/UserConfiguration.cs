using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserService.Persistance.PostgreSQL.Entities;

namespace UserService.Persistance.PostgreSQL.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<UserEntity>
    {
        public void Configure(EntityTypeBuilder<UserEntity> builder)
        {
            builder.HasKey(u => u.Id);

            builder.HasOne(u => u.PersonProfile)
                .WithOne(p => p.User)
                .HasForeignKey<PersonProfileEntity>(p => p.UserId);


            builder.HasOne(u => u.CompanyProfile)
                .WithOne(c => c.User)
                .HasForeignKey<CompanyProfileEntity>(p => p.UserId);
            ;
        }
    }

    public class PersonPeofileConfiguration : IEntityTypeConfiguration<PersonProfileEntity>
    {
        public void Configure(EntityTypeBuilder<PersonProfileEntity> builder)
        {
            builder.HasKey(p => p.UserId);

            builder.HasOne(p => p.User)
                .WithOne(u => u.PersonProfile);
        }
    }

    public class CompanyProfileConfiguration : IEntityTypeConfiguration<CompanyProfileEntity>
    {
        public void Configure(EntityTypeBuilder<CompanyProfileEntity> builder)
        {
            builder.HasKey(p => p.UserId);

            builder.HasOne(p => p.User)
                .WithOne(u => u.CompanyProfile);
        }
    }
}
