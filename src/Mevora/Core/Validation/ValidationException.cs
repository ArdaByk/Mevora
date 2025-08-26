namespace Mevora;

public class ValidationException : Exception
{
    public IReadOnlyList<string> Errors { get; }

    public ValidationException(string message) : base(message)
    {
        Errors = new List<string> { message };
    }

    public ValidationException(IEnumerable<string> errors)
        : base($"Validation failed: {string.Join(", ", errors)}")
    {
        Errors = errors.ToList().AsReadOnly();
    }
}
