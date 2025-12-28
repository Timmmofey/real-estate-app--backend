namespace UserService.Application.Exeptions
{
    public class UserAlreadyExistsException : Exception
    {
        public UserAlreadyExistsException()
            : base() { }
    }
}
