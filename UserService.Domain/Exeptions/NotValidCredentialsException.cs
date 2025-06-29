namespace UserService.Domain.Exeptions
{
    public class NotValidCredentialsException : Exception
    {
        public NotValidCredentialsException()
            : base("Provided credentials are not valid.") { }
    }
}
