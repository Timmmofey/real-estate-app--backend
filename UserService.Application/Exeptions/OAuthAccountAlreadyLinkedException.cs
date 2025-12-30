namespace UserService.Application.Exeptions
{
    public class OAuthAccountAlreadyLinkedException : Exception
    {
        public OAuthAccountAlreadyLinkedException()
            : base("User alredy has OAuth account connected with this provider.") { }
    }
}
