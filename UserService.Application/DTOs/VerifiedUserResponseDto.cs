using Classified.Shared.Constants;

namespace UserService.Application.DTOs
{
    public class VerifiedUserResponseDto
    {
        public Guid Id { get; set; }
        public UserRole Role { get; set; }
        public bool IsDeleted { get; set; }
    }
}
