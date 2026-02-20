namespace AuthService.Domain.Consts
{
    public static class LoginStatus
    {
        public const string Success = "Success";
        public const string Restore = "Restore";
        public const string TwoFactor = "TwoFactor";
        public const string InvalidCredentials = "InvalidCredentials";
        public const string Blocked = "Blocked";
    }
}
