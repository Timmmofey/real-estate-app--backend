namespace UserService.Domain.Consts
{
    public static class RedisKey
    {
        public const string ToggleTwoStepAuthCaode = "toggle-two-step-auth-code";
        public const string PasswordReset = "pwd-reset";
        public const string CurrentEmailConfirmationCode = "current-email-cofirmation-code";
        public const string NewEmailCofirmationCode = "new-email-cofirmation-code";

    }
}
