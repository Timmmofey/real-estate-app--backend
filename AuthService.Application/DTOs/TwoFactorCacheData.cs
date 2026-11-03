using Classified.Shared.Constants;

namespace UserService.Application.DTOs
{
    public record TwoFactorCacheData(
        string Code,
        UserRole UserRole,
        int RemainingAttempts
    );
}
