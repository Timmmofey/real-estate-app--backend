namespace UserService.Application.Exeptions
{
    public class NotValidCredentialsException : Exception
    {
        public NotValidCredentialsException()
            : base("Provided credentials are not valid.") { }
    }
}
