using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Mevora;

namespace Mevora.UnitTests;

public class ExceptionTestCommand : IRequest { }

public class ExceptionTestCommandHandler : IRequestProcessorAsync<ExceptionTestCommand>
{
    public Task ProcessAsync(ExceptionTestCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException("Handler error");
    }
}

public class ExceptionHandlingTests
{
    [Fact]
    public async Task HandlerException_Should_BubbleUp_WithoutLosingStackTrace()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMevora(cfg => cfg.AddProcessorsFromAssembly(typeof(ServiceProviderFactory).Assembly));
        services.AddTransient<IRequestProcessorAsync<ExceptionTestCommand>, ExceptionTestCommandHandler>();
        services.AddMevoraDispatcher();

        var sp = services.BuildServiceProvider();
        var dispatcher = sp.GetRequiredService<IMevoraDispatcher>();

        // Act
        Func<Task> act = async () => await dispatcher.DispatchAsync(new ExceptionTestCommand());

        // Assert
        var ex = await act.Should().ThrowAsync<NotImplementedException>();
        ex.WithMessage("Handler error");
    }
}
