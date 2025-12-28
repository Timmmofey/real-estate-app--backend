
using Classified.Shared.Entities;

namespace UserService.Persistance.PostgreSQL.Entities
{
    public class UserEntity
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = default!;
        public string? PasswordHash { get; set; }
        public string PhoneNumber { get; set; } = default!;
        public UserRoleEntity Role { get; set; }

        public bool IsTwoFactorEnabled { get; set; } = false;
        public bool IsVerified { get; set; } = false;
        public bool IsBlocked { get; set; } = false;
        public bool IsSoftDeleted { get; set; } = false;
        public bool IsPermanantlyDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } 
        public DateTime? DeletedAt { get; set; }

        public PersonProfileEntity? PersonProfile { get; set; }
        public CompanyProfileEntity? CompanyProfile { get; set; }
        public List<UserOAuthAccountEntity>? UserOAuthAccounts { get; set;  }
    }
}
