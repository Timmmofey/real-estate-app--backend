using Classified.Shared.Constants;
using Classified.Shared.Validation;

namespace UserService.Domain.Models
{
    public class User
    {
        public Guid Id { get; }
        public string Email { get; } = default!;
        public string PhoneNumber { get; } = default!;
        public UserRole Role { get; }

        public bool IsTwoFactorEnabled { get; } = false;
        public bool IsVerified { get; } = false;
        public bool IsBlocked { get; } = false;
        public bool IsSoftDeleted { get; } = false;
        public bool IsPermanantlyDeleted { get; } = false;

        public string? PasswordHash { get; }
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
        public DateTime? DeletedAt { get; }

        public PersonProfile? PersonProfile { get; }
        public CompanyProfile? CompanyProfile { get; }

        private User(Guid id, string email, string phoneNumber, string? passwordHash, UserRole role)
        {
            Id = id;
            Email = email;
            PasswordHash = passwordHash;
            PhoneNumber = phoneNumber;
            Role = role;
        }

        private User(Guid id, string email, string phoneNumber, UserRole role, string? passwordHash, bool? isTwoFactorEnabled, bool? isVerified, bool? isBlocked, bool? isSoftDeleted, bool? isPermanantlyDeleted, DateTime? createdAt, DateTime? deletedAt)
        {
            Id = id;
            Email = email;
            PasswordHash = passwordHash;
            PhoneNumber = phoneNumber;
            Role = role;
            IsTwoFactorEnabled = isTwoFactorEnabled ?? false;
            IsVerified = isVerified ?? false;
            IsBlocked = isBlocked ?? false;
            IsSoftDeleted = isSoftDeleted ?? false;
            IsPermanantlyDeleted = isPermanantlyDeleted ?? false;
            CreatedAt = createdAt ?? DateTime.UtcNow;
            DeletedAt = deletedAt;
        }

        private static string? ValidateUserInputs(Guid id, string email, string phoneNumber)
        {
            if (id == Guid.Empty)
                return "Id cannot be empty.";

            if (string.IsNullOrWhiteSpace(email))
                return "Email cannot be empty.";

            if (!Validators.IsValidEmail(email))
                return "Email format is invalid.";

            if (string.IsNullOrWhiteSpace(phoneNumber))
                return "Phone number cannot be empty.";

            if (!Validators.IsValidPhoneNumber(phoneNumber))
                return "Phone number format is invalid.";

            return null;
        }

        public static (User? User, string? Error) CreateNew(Guid id, string email, string phoneNumber, string? passwordHash, UserRole role)
        {
            var validationError = ValidateUserInputs(id, email, phoneNumber);
            if (validationError != null)
                return (null, validationError);

            var user = new User(id, email, phoneNumber, passwordHash, role);
            return (user, null);
        }

        public static (User? User, string? Error) CreateExisting(
            Guid id,
            string email,
            string phoneNumber,
            UserRole role,
            string? passwordHash,
            bool? isTwoFactorEnabled,
            bool? isVerified,
            bool? isBlocked,
            bool? isSoftDeleted,
            bool? isPermanantlyDeleted,
            DateTime? createdAt,
            DateTime? deletedAt)
        {
            var validationError = ValidateUserInputs(id, email, phoneNumber);
            if (validationError != null)
                return (null, validationError);

            var user = new User(id, email, phoneNumber, role, passwordHash, isTwoFactorEnabled, isVerified, isBlocked, isSoftDeleted, isPermanantlyDeleted, createdAt, deletedAt);
            return (user, null);
        }
    }
}