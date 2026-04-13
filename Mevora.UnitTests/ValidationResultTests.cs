using FluentAssertions;
using Mevora;

namespace Mevora.UnitTests;

/// <summary>
/// ValidationResult statik fabrika metodlarının testleri.
/// </summary>
public class ValidationResultTests
{
    [Fact]
    public void Success_Should_ReturnValidResult()
    {
        var result = ValidationResult.Success();

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Failure_WithList_Should_ReturnInvalidResult()
    {
        var errors = new List<string> { "Error1", "Error2" };
        var result = ValidationResult.Failure(errors);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().BeEquivalentTo(errors);
    }

    [Fact]
    public void Failure_WithParams_Should_ReturnInvalidResult()
    {
        var result = ValidationResult.Failure("Err A", "Err B");

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().Contain("Err A");
        result.Errors.Should().Contain("Err B");
    }

    [Fact]
    public void Failure_ShouldBe_Immutable()
    {
        // IReadOnlyList olduğu için mutasyona izin vermemeli
        var result = ValidationResult.Failure("Error");
        var act = () => ((IList<string>)result.Errors).Add("Extra");

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void ToString_Success_Should_ContainSucceeded()
    {
        var result = ValidationResult.Success();
        result.ToString().Should().Contain("succeeded");
    }

    [Fact]
    public void ToString_Failure_Should_ContainErrors()
    {
        var result = ValidationResult.Failure("MissingField");
        result.ToString().Should().Contain("MissingField");
    }
}
