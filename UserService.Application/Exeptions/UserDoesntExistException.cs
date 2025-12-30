namespace UserService.Application.Exeptions
{
    public class UserDoesntExistException : Exception
    {
        public UserDoesntExistException()
            : base("User doens`t exist.") { }
    }
}
