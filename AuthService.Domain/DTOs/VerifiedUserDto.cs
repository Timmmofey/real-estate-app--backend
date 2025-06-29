using Classified.Shared.Constants;

namespace AuthService.Domain.DTOs
{
    public class VerifiedUserDto
    {
        public Guid Id { get; set; }
        public UserRole Role { get; set; }
        public bool IsDeleted { get; set; }
    }
}