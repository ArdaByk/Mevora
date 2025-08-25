using Mevora.Models.Requests;

namespace Mevora.Core.Validation.Validators;

public class TestQueryValidator : IRequestValidator<TestQuery>
{
    public ValidationResult Validate(ValidationContext<TestQuery> context)
    {
        return context.CheckNotEmpty(x => x.Name, "Ad alanı boş olmamalı.")
            .ToResult();
    }
}
