namespace UserService.Domain.Exeptions
{
    public class RecentlyDeletedUserExceptionOnLogin : Exception
    {
        public RecentlyDeletedUserExceptionOnLogin()
            : base("This account was recently deleted. You can restore your account or delete completely.") { }
    }
}
