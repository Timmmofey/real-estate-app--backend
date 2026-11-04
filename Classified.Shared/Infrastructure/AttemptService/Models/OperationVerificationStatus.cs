namespace Classified.Shared.Infrastructure.AttemptService.Models
{
    public enum OperationVerificationStatus
    {
        NotFound = 0,
        InvalidCode = 1,
        AttemptsExceeded = 2,
        InvalidOperationData = 3,
        Success = 4
    }
}
