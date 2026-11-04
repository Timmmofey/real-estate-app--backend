using Classified.Shared.Constants;
using Classified.Shared.Infrastructure.AttemptService.Interfaces;

namespace UserService.Application.DTOs
{
    public sealed record TwoFactorCacheData(string Code, UserRole UserRole) : ICodeOperationData;
}
