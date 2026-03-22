namespace Classified.Shared.Extensions.ErrorHandler.Errors
{
    public sealed class ValidationErrors
    {
        private readonly Dictionary<string, string[]> _errors = new();

        public Dictionary<string, string[]> Errors => _errors;

        public void Add(string field, string message)
        {
            _errors[field] = new[] { message };
        }

        public bool Any() => _errors.Count > 0;
    }

    public class DomainValidationException : Exception
    {
        public Dictionary<string, string[]> Errors { get; }

        // Одно поле
        public DomainValidationException(string field, string error)
            : base("Validation failed")
        {
            Errors = new Dictionary<string, string[]>
            {
                [field] = new[] { error }
            };
        }

        public DomainValidationException(string message)
           : base(message)
        {
            Errors = new Dictionary<string, string[]>();
        }

        // Несколько полей
        public DomainValidationException(Dictionary<string, string[]> errors, string message = "Validation failed")
            : base(message)
        {
            Errors = errors;
        }
    }
}
