using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mevora;


public class ValidationResult
{
    public bool IsValid { get; }
    public IReadOnlyList<string> Errors { get; }

    private ValidationResult(bool isValid, IEnumerable<string>? errors = null)
    {
        IsValid = isValid;
        Errors = errors?.ToList().AsReadOnly() ?? new List<string>().AsReadOnly();
    }

    public static ValidationResult Success()
        => new ValidationResult(true);

    public static ValidationResult Failure(IEnumerable<string> errors)
        => new ValidationResult(false, errors);

    public static ValidationResult Failure(params string[] errors)
        => new ValidationResult(false, errors);

    public override string ToString()
    {
        return IsValid ? "Validation succeeded." : $"Validation failed: {string.Join(", ", Errors)}";
    }
}
