using Classified.Shared.Infrastructure.AttemptService.Models;

namespace Classified.Shared.Infrastructure.AttemptService.Interfaces
{
    public interface IAttemptService
    {
        /// <summary>
        /// Создаёт бизнес-операцию, сохраняет её данные,
        /// устанавливает максимальное количество попыток и TTL.
        /// Существующий счётчик не обнуляется.
        /// </summary>
        Task InitializeOperationAsync<TData>(
            string operationKey,
            string subjectId,
            TData data,
            int maxAttempts,
            TimeSpan expiration,
            CancellationToken ct)
            where TData : class, ICodeOperationData;

        /// <summary>
        /// Проверяет код, контролирует количество попыток.
        /// При успехе удаляет операцию и её счётчики.
        /// </summary>
        Task<OperationVerificationResult<TData>> VerifyCodeAsync<TData>(
            string operationKey,
            string subjectId,
            string code,
            CancellationToken ct)
            where TData : class, ICodeOperationData;

        /// <summary>
        /// Удаляет операцию вместе со всеми её счётчиками.
        /// </summary>
        Task ResetOperationAsync(
            string operationKey,
            string subjectId,
            CancellationToken ct);
    }
}
