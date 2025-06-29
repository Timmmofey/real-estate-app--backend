namespace UserService.Domain.Exeptions
{
    public class BlockedUserAccountException : Exception
    {
        public BlockedUserAccountException()
            : base("This account was banned due to platform violations.") { }
    }
}
