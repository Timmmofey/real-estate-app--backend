using Microsoft.AspNetCore.Authorization;

namespace Classified.Shared.Extensions.ServerJwtAuth
{
    public class InternalAuthorizeServerJwtRequirement : IAuthorizationRequirement
    {
        public string[] AllowedServices { get; }

        public InternalAuthorizeServerJwtRequirement(params string[] allowedServices)
        {
            AllowedServices = allowedServices ?? Array.Empty<string>();
        }
    }
}
