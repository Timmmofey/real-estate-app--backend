namespace Classified.Shared.Extensions.ErrorHandler.Errors
{
    public class ForbiddenAccessException : Exception
    {
        public ForbiddenAccessException(string message = "Forbidden") : base(message) { }
    }
}
