using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Mevora;

namespace Mevora.UnitTests;

/// <summary>
/// ValidationBehavior testleri:
/// Hatalı istek Pipeline ve Handler'a hiç ulaşmamalıdır (Short-circuit).
/// </summary>
public class ValidationBehaviorTests
{
    private static IMevoraDispatcher BuildDispatcher(bool withPipeline = false)
    {
        var tracker = new List<string>();
        var services = new ServiceCollection();

        services.AddMevora(cfg =>
            cfg.AddProcessorsFromAssembly(typeof(ServiceProviderFactory).Assembly));

        if (withPipeline)
            services.AddTransient<IPipelineAction<ValidatableRequest, string>>(
                _ => new TrackingPipelineA<ValidatableRequest, string>(tracker));

        services.AddMevoraDispatcher();

        return services.BuildServiceProvider().GetRequiredService<IMevoraDispatcher>();
    }

    [Fact]
    public async Task FailingValidator_Should_ThrowValidationException()
    {
        // Arrange — Name boş bırakılıyor → Validator hata üretmeli
        var dispatcher = BuildDispatcher();
        var request = new ValidatableRequest { Name = "", Age = 25 };

        // Act
        var act = async () => await dispatcher.DispatchAsync(request);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task FailingValidator_Should_ShortCircuit_Pipeline()
    {
        // Arrange — Pipeline kayıtlı ama validator reddetmeli → Handler çalışmamalı
        ValidatableRequestHandler.CallCount = 0;
        var dispatcher = BuildDispatcher(withPipeline: true);
        var invalidRequest = new ValidatableRequest { Name = "", Age = 25 };

        // Act
        var act = async () => await dispatcher.DispatchAsync(invalidRequest);

        // Assert — exception fırlamalı ve handler callcount sıfır kalmalı
        await act.Should().ThrowAsync<ValidationException>();
        ValidatableRequestHandler.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task ValidRequest_Should_PassValidation_And_Execute()
    {
        // Arrange — geçerli istek → handler çalışmalı
        ValidatableRequestHandler.CallCount = 0;
        var dispatcher = BuildDispatcher();
        var validRequest = new ValidatableRequest { Name = "Arda", Age = 25 };

        // Act
        var result = await dispatcher.DispatchAsync(validRequest);

        // Assert
        result.Should().Be("ok");
        ValidatableRequestHandler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task FailingValidator_Should_ContainErrors()
    {
        // Arrange — iki hata: boş Name + geçersiz Age
        var dispatcher = BuildDispatcher();
        var request = new ValidatableRequest { Name = "", Age = 999 };

        // Act
        var act = async () => await dispatcher.DispatchAsync(request);

        // Assert — her iki hata da ValidationException içinde bulunmalı
        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().HaveCountGreaterOrEqualTo(2);
    }
}
