namespace UserService.Application.Exeptions
{
    public class UserNotFoundException : Exception
    {
        public UserNotFoundException()
            : base("User doesn`t exust.") { }
    }
}
