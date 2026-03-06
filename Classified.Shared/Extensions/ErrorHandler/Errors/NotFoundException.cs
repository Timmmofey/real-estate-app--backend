namespace Classified.Shared.Extensions.ErrorHandler.Errors
{
    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message) { }
    }
}
