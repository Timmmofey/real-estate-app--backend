namespace UserService.Application.Exeptions
{
    public class OAuthAccountNotFoundException : Exception
    {
        public OAuthAccountNotFoundException()
            : base("OAuth account hag not been found exception") { }
    }
}
