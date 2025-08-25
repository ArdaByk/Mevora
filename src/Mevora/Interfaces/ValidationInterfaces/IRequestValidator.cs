namespace Mevora;

public interface IRequestValidator<TRequest>
    where TRequest : IRequest
{
    ValidationResult Validate(ValidationContext<TRequest> context);
}