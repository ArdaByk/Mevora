using FluentAssertions;
using Mevora;

namespace Mevora.UnitTests;

/// <summary>
/// ValidationContext çekirdek testleri:
/// CheckNotEmpty, CheckMinLength, CheckRange, CheckRegex kurallarının
/// hatalı verilerde listeye doğru hata eklediği doğrulanır.
/// Reset() ile pool'daki önceki hataların temizlendiği kanıtlanır.
/// </summary>
public class ValidationContextTests
{
    // ────────────────────────────────────────────
    //  Test tipi
    // ────────────────────────────────────────────
    private record TestModel(string? Name, string? Email, int Age);

    // ────────────────────────────────────────────
    //  CheckNotEmpty
    // ────────────────────────────────────────────

    [Fact]
    public void CheckNotEmpty_ShouldAddError_WhenEmpty()
    {
        var ctx = new ValidationContext<TestModel>(new TestModel("", null, 25));
        var result = ctx.CheckNotEmpty(m => m.Name, "Name is required").ToResult();

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Name is required");
    }

    [Fact]
    public void CheckNotEmpty_ShouldAddError_WhenWhitespace()
    {
        var ctx = new ValidationContext<TestModel>(new TestModel("   ", null, 25));
        var result = ctx.CheckNotEmpty(m => m.Name, "Name is required").ToResult();

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void CheckNotEmpty_ShouldNotAddError_WhenHasValue()
    {
        var ctx = new ValidationContext<TestModel>(new TestModel("Arda", null, 25));
        var result = ctx.CheckNotEmpty(m => m.Name, "Name is required").ToResult();

        result.IsValid.Should().BeTrue();
    }

    // ────────────────────────────────────────────
    //  CheckMinLength
    // ────────────────────────────────────────────

    [Fact]
    public void CheckMinLength_ShouldAddError_WhenTooShort()
    {
        var ctx = new ValidationContext<TestModel>(new TestModel("ab", null, 25));
        var result = ctx.CheckMinLength(m => m.Name, 5, "Name too short").ToResult();

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Name too short");
    }

    [Fact]
    public void CheckMinLength_ShouldNotAddError_WhenLongEnough()
    {
        var ctx = new ValidationContext<TestModel>(new TestModel("Arda Byk", null, 25));
        var result = ctx.CheckMinLength(m => m.Name, 3, "Name too short").ToResult();

        result.IsValid.Should().BeTrue();
    }

    // ────────────────────────────────────────────
    //  CheckRange
    // ────────────────────────────────────────────

    [Fact]
    public void CheckRange_ShouldAddError_WhenBelowMin()
    {
        var ctx = new ValidationContext<TestModel>(new TestModel("Arda", null, -1));
        var result = ctx.CheckRange(m => m.Age, 0, 150, "Age out of range").ToResult();

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Age out of range");
    }

    [Fact]
    public void CheckRange_ShouldAddError_WhenAboveMax()
    {
        var ctx = new ValidationContext<TestModel>(new TestModel("Arda", null, 200));
        var result = ctx.CheckRange(m => m.Age, 0, 150, "Age out of range").ToResult();

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void CheckRange_ShouldNotAddError_WhenInRange()
    {
        var ctx = new ValidationContext<TestModel>(new TestModel("Arda", null, 25));
        var result = ctx.CheckRange(m => m.Age, 0, 150, "Age out of range").ToResult();

        result.IsValid.Should().BeTrue();
    }

    // ────────────────────────────────────────────
    //  CheckRegex
    // ────────────────────────────────────────────

    [Fact]
    public void CheckRegex_ShouldAddError_WhenNotMatching()
    {
        var ctx = new ValidationContext<TestModel>(new TestModel("Arda", "not_an_email", 25));
        var result = ctx
            .CheckRegex(m => m.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", "Invalid email")
            .ToResult();

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Invalid email");
    }

    [Fact]
    public void CheckRegex_ShouldNotAddError_WhenMatching()
    {
        var ctx = new ValidationContext<TestModel>(new TestModel("Arda", "arda@example.com", 25));
        var result = ctx
            .CheckRegex(m => m.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", "Invalid email")
            .ToResult();

        result.IsValid.Should().BeTrue();
    }

    // ────────────────────────────────────────────
    //  Reset() — pooling stratejisi
    // ────────────────────────────────────────────

    [Fact]
    public void Reset_Should_ClearPreviousErrors()
    {
        // İlk isteği: hatalı model
        var ctx = new ValidationContext<TestModel>(new TestModel("", null, 25));
        ctx.CheckNotEmpty(m => m.Name, "Name is required");
        var firstResult = ctx.ToResult();
        firstResult.IsValid.Should().BeFalse("ilk istekte hata bekleniyor");

        // Pool'dan geri al, Reset ile yenile
        ctx.Reset(new TestModel("Arda", null, 25));

        // İkinci isteği: geçerli model — önceki hata listesi temizlenmiş olmalı
        var secondResult = ctx
            .CheckNotEmpty(m => m.Name, "Name is required")
            .ToResult();

        secondResult.IsValid.Should().BeTrue("Reset() çağrısı hata listesini temizlemiş olmalı");
        secondResult.Errors.Should().BeEmpty();
    }

    // ────────────────────────────────────────────
    //  Çoklu kural zincirleme
    // ────────────────────────────────────────────

    [Fact]
    public void MultipleRules_ShouldCollectAllErrors()
    {
        var ctx = new ValidationContext<TestModel>(new TestModel("", "bad", -5));
        var result = ctx
            .CheckNotEmpty(m => m.Name, "Name is required")
            .CheckRegex(m => m.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", "Invalid email")
            .CheckRange(m => m.Age, 0, 150, "Age out of range")
            .ToResult();

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(3);
    }
}
