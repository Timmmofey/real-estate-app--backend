using Classified.Shared.Constants;

namespace Classified.Shared.DTOs
{
    public class VerifiedUserDto
    {
        public Guid Id { get; set; }
        public UserRole Role { get; set; } = default!;
        public string Email { get; set; } = default!;
        public bool IsDeleted { get; set; }
        public bool IsTwoFactorEnabled { get; set; }
    }
}
