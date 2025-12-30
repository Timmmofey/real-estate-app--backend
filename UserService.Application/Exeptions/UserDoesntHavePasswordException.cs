using Classified.Shared.Constants;

namespace UserService.Application.Exeptions
{
    public class UserDoesntHavePasswordException : Exception
    {
        public UserDoesntHavePasswordException()
            : base("User doens`t have a password.") { }
    }
}
