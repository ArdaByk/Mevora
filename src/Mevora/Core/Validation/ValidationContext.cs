using System.Text.RegularExpressions;

namespace Mevora;

public class ValidationContext<T>
{
    private T _instance;
    private readonly List<string> _errors = new();

    public ValidationContext(T instance)
    {
        _instance = instance;
    }

    public void Reset(T instance)
    {
        _instance = instance;
        _errors.Clear();
    }

    /// <summary>
    /// Genel amaçlı kontrol
    /// </summary>
    public ValidationContext<T> Check(Func<T, bool> predicate, string errorMessage)
    {
        if (!predicate(_instance))
            _errors.Add(errorMessage);
        return this;
    }

    /// <summary>
    /// String alanın boş olup olmadığını kontrol eder
    /// </summary>
    public ValidationContext<T> CheckNotEmpty(Func<T, string?> selector, string errorMessage)
    {
        var value = selector(_instance);
        if (string.IsNullOrWhiteSpace(value))
            _errors.Add(errorMessage);
        return this;
    }

    /// <summary>
    /// String uzunluğunu kontrol eder
    /// </summary>
    public ValidationContext<T> CheckMinLength(Func<T, string?> selector, int minLength, string errorMessage)
    {
        var value = selector(_instance);
        if (string.IsNullOrEmpty(value) || value.Length < minLength)
            _errors.Add(errorMessage);
        return this;
    }

    /// <summary>
    /// Regex pattern eşleşmesini kontrol eder
    /// </summary>
    public ValidationContext<T> CheckRegex(Func<T, string?> selector, string pattern, string errorMessage)
    {
        var value = selector(_instance);
        if (string.IsNullOrEmpty(value) || !Regex.IsMatch(value, pattern))
            _errors.Add(errorMessage);
        return this;
    }

    /// <summary>
    /// Sayı aralığını kontrol eder
    /// </summary>
    public ValidationContext<T> CheckRange(Func<T, int> selector, int min, int max, string errorMessage)
    {
        var value = selector(_instance);
        if (value < min || value > max)
            _errors.Add(errorMessage);
        return this;
    }

    public ValidationResult ToResult()
    {
        return _errors.Any()
            ? ValidationResult.Failure(_errors)
            : ValidationResult.Success();
    }
}
