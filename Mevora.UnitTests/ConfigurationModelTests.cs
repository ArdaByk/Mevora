using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Mevora;

namespace Mevora.UnitTests;

/// <summary>
/// ConfigurationModel ve DI ekosistemi testleri.
/// </summary>
public class ConfigurationModelTests
{
    // ────────────────────────────────────────────
    //  AddPipelineAction — generic olmayan tip exception fırlatmalı
    // ────────────────────────────────────────────

    [Fact]
    public void AddPipelineAction_NonGenericType_Should_ThrowArgumentException()
    {
        // Arrange — kapalı (closed) bir generic tip: generic type definition değil
        var config = new ConfigurationModel();
        var closedType = typeof(TrackingPipelineA<PingRequest, PingResponse>); // <--- kapalı

        // Act
        var act = () => config.AddPipelineAction(closedType);

        // Assert
        act.Should().Throw<ArgumentException>()
           .WithMessage("*open generic*");
    }

    [Fact]
    public void AddPipelineAction_NonGenericClass_Should_ThrowArgumentException()
    {
        // Arrange — hiç generic parametresi olmayan yanlış tip
        var config = new ConfigurationModel();

        // Act
        var act = () => config.AddPipelineAction(typeof(string));

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddPipelineAction_ValidOpenGeneric_Should_NotThrow()
    {
        // Arrange — açık generic tip (2 type arg → IPipelineAction<,>)
        var config = new ConfigurationModel();

        // Act
        var act = () => config.AddPipelineAction(typeof(TrackingPipelineA<,>));

        // Assert
        act.Should().NotThrow();
    }

    // ────────────────────────────────────────────
    //  AddProcessorsFromAssembly — boş assembly listesiyle exception
    // ────────────────────────────────────────────

    [Fact]
    public void AddMevora_WithNoAssembly_Should_ThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        var act = () => services.AddMevora(cfg => { /* empty config */ });
        act.Should().Throw<ArgumentException>()
            .WithMessage("*No assemblies were registered*");
    }

    // ────────────────────────────────────────────
    //  WithServiceLifetime — Singleton yaşam döngüsü
    // ────────────────────────────────────────────

    [Fact]
    public void WithServiceLifetime_Singleton_Should_ReturnSameHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMevora(cfg =>
        {
            cfg.AddProcessorsFromAssembly(typeof(ServiceProviderFactory).Assembly);
            cfg.WithServiceLifetime(ServiceLifetime.Singleton);
        });
        services.AddMevoraDispatcher();
        var sp = services.BuildServiceProvider();

        // Act
        var h1 = sp.GetRequiredService<IRequestProcessorAsync<PingRequest, PingResponse>>();
        var h2 = sp.GetRequiredService<IRequestProcessorAsync<PingRequest, PingResponse>>();

        // Assert — Singleton → aynı örnek
        h1.Should().BeSameAs(h2);
    }

    [Fact]
    public void DefaultLifetime_Transient_Should_ReturnDifferentHandlers()
    {
        // Arrange — varsayılan Transient
        var services = new ServiceCollection();
        services.AddMevora(cfg =>
            cfg.AddProcessorsFromAssembly(typeof(ServiceProviderFactory).Assembly));
        services.AddMevoraDispatcher();
        var sp = services.BuildServiceProvider();

        // Act
        var h1 = sp.GetRequiredService<IRequestProcessorAsync<PingRequest, PingResponse>>();
        var h2 = sp.GetRequiredService<IRequestProcessorAsync<PingRequest, PingResponse>>();

        // Assert — Transient → farklı örnekler
        h1.Should().NotBeSameAs(h2);
    }

    // ────────────────────────────────────────────
    //  DI kayıt doğrulaması
    // ────────────────────────────────────────────

    [Fact]
    public void AddMevora_Should_Register_RequestProcessor()
    {
        var services = new ServiceCollection();
        services.AddMevora(cfg =>
            cfg.AddProcessorsFromAssembly(typeof(ServiceProviderFactory).Assembly));
        var sp = services.BuildServiceProvider();

        var handler = sp.GetService<IRequestProcessorAsync<PingRequest, PingResponse>>();
        handler.Should().NotBeNull();
        handler.Should().BeOfType<PingRequestHandler>();
    }

    [Fact]
    public void AddMevora_Should_Register_MessageProcessor()
    {
        var services = new ServiceCollection();
        services.AddMevora(cfg =>
            cfg.AddProcessorsFromAssembly(typeof(ServiceProviderFactory).Assembly));
        var sp = services.BuildServiceProvider();

        var handlers = sp.GetServices<IMessageProcessor<OrderCreatedMessage>>().ToList();
        handlers.Should().HaveCount(2, "iki handler kayıtlı olmalı: HandlerA ve HandlerB");
    }

    [Fact]
    public void AddMevora_Should_Register_Validator()
    {
        var services = new ServiceCollection();
        services.AddMevora(cfg =>
            cfg.AddProcessorsFromAssembly(typeof(ServiceProviderFactory).Assembly));
        var sp = services.BuildServiceProvider();

        var validator = sp.GetService<IRequestValidator<ValidatableRequest>>();
        validator.Should().NotBeNull();
        validator.Should().BeOfType<ValidatableRequestValidator>();
    }
}
