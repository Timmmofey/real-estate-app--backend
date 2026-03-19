using Classified.Shared.Constants;

namespace Classified.Shared.Infrastructure.MicroserviceJwt
{
    public interface IMicroserviceJwtProvider
    {
        string GenerateToken(string audience, string? subject = null, int expiresMinutes = 1);
    }
}
