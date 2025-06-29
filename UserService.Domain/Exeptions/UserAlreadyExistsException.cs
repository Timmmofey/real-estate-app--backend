namespace UserService.Domain.Exeptions
{
    public class UserAlreadyExistsException : Exception
    {
        public UserAlreadyExistsException()
            : base("A user with this email or phone number already exists.") { }
    }
}
