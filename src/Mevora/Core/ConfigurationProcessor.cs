using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace Mevora;

internal static class ConfigurationProcessor
{

    internal static void RegisterProcessors(ConfigurationModel configurationModel, IServiceCollection serviceCollection)
    {
        List<Assembly> assemblies = configurationModel.ProcessorsToBeRegistered;

        if (!assemblies.Any())
            throw new ArgumentException("No assemblies were registered. Use AddProcessorsFromAssembly or AddProcessorsFromAssemblies in AddMevora.");

        var types = assemblies.SelectMany(assembly => assembly.GetTypes()).Where(i => !i.IsInterface);

        var requestProcessors = types.Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IRequestProcessorAsync<,>) || t.GetGenericTypeDefinition() == typeof(IRequestProcessor<,>)).ToList();

        foreach (var requestProcessor in requestProcessors)
        {

            var processor = requestProcessor.GetInterfaces().FirstOrDefault();

            var processorRequest = requestProcessor.GetGenericArguments()[0];
            var processorResponse = requestProcessor.GetGenericArguments()[1];

            var genericType = processor.MakeGenericType(processorRequest, processorResponse);

            serviceCollection.TryAdd(new ServiceDescriptor(genericType, configurationModel.Lifetime));
        }

        serviceCollection.AddSingleton<IMevoraDispatcher, MevoraDispatcher>();

    }
}
