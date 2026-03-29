//using Classified.Shared.Constants;
//using Classified.Shared.DTOs;

//namespace AuthService.Domain.Abstactions
//{
//    public interface IUserServiceClient
//    {
//        Task<VerifiedUserDto?> VerifyUserCredentialsAsync(string phoneOrEmail, string password, CancellationToken ct);
//        Task<VerifiedUserDto?> GetVerifiedUserDtoByIdAsync(string userId, CancellationToken ct);
//        Task<UserOAuthAccountDto?> GetUserOAuthAccountByProviderAndProviderUserIdAsync(OAuthProvider provider, string providerUserId, CancellationToken ct);
//        Task<string?> GetUserIdByEmailAsync(string email, CancellationToken ct);
//        Task ConnectOauthAccountToExistingUserAsync(OAuthProvider provider, string providerId, Guid userId, CancellationToken ct);
//    }
//}
