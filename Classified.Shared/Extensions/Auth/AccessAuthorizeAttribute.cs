using Classified.Shared.Constants;
using Microsoft.AspNetCore.Authorization;

namespace Classified.Shared.Extensions.Auth
{
    public class AccessAuthorizeAttribute : AuthorizeAttribute
    {
        public AccessAuthorizeAttribute()
        {
            Policy = nameof(JwtTokenType.Access);
        }
    }
}
