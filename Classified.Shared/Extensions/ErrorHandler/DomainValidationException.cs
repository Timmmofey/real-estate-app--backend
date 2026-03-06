namespace Classified.Shared.Extensions.ErrorHandler
{
    public class DomainValidationException : Exception
    {
        public Dictionary<string, string[]> Errors { get; }

        public DomainValidationException(string message, Dictionary<string, string[]> errors)
            : base(message)
        {
            Errors = errors;
        }

        public DomainValidationException(string field, string error)
            : this("Validation failed", new Dictionary<string, string[]> { [field] = new[] { error } })
        { }
    }

}
