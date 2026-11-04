namespace Classified.Shared.Infrastructure.AttemptService.Models
{
    public sealed record OperationVerificationResult<TData>(
        OperationVerificationStatus Status,
        TData? Data,
        int FailedAttempts,
        int RemainingAttempts
    );
}
