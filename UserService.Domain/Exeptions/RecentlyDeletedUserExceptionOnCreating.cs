namespace UserService.Domain.Exeptions
{
    public class RecentlyDeletedUserExceptionOnCreating : Exception
    {
        public RecentlyDeletedUserExceptionOnCreating()
            : base("This account was recently deleted. Please restore your account instead of creating a new one.") { }
    }
}
