namespace AuthService.Application.Exceptions
{
    public class InvalidTwoFactorCodeException : Exception
    {
        public int RemainingAttempts { get; }

        public InvalidTwoFactorCodeException(int remainingAttempts)
            : base($"Invalid verification code. Remaining attempts: {remainingAttempts}")
        {
            RemainingAttempts = remainingAttempts;
        }
    }

}
