namespace Classified.Shared.Infrastructure.AttemptService.Models
{
    public sealed record AttemptStatus(
        int FailedAttempts,
        int RemainingAttempts,
        bool IsBlocked
    );
}
