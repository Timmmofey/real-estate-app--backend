using Classified.Shared.Constants;

namespace Classified.Shared.Infrastructure.MicroserviceJwt
{
    public interface IMicroserviceJwtProvider
    {
        string GenerateToken(string subject, string audience, int expiresMinutes = 3);
    }
}
