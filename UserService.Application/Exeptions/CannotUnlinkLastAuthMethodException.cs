namespace UserService.Application.Exeptions
{
    public class CannotUnlinkLastAuthMethodException : Exception
    {
        public CannotUnlinkLastAuthMethodException()
            : base("User have only one auth method.") { }
    }
}
