using Microsoft.Extensions.DependencyInjection;
using Mevora.Core.Configuration;
using Mevora.Abstractions.Configuration;

namespace Mevora;

internal static class ConfigurationProcessor
{
    private static readonly IServiceRegistrar[] _registrars = 
    {
        new ProcessorRegistrar(),
        new PipelineRegistrar()
    };

    internal static void RegisterProcessors(ConfigurationModel configurationModel, IServiceCollection serviceCollection)
    {
        foreach (var registrar in _registrars)
        {
            registrar.RegisterServices(serviceCollection, configurationModel);
        }
    }
}
