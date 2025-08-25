using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;
using Mevora.Abstractions.Configuration;

namespace Mevora.Core.Configuration;

internal class ProcessorRegistrar : IServiceRegistrar
{
    public void RegisterServices(IServiceCollection serviceCollection, ConfigurationModel configurationModel)
    {
        var assemblies = configurationModel.ProcessorsToBeRegistered;

        if (!assemblies.Any())
            throw new ArgumentException("No assemblies were registered. Use AddProcessorsFromAssembly or AddProcessorsFromAssemblies in AddMevora.");

        var types = assemblies.SelectMany(assembly => assembly.GetTypes()).Where(i => !i.IsInterface);

        RegisterRequestProcessors(serviceCollection, configurationModel, types);
        RegisterMessageProcessors(serviceCollection, configurationModel, types);
        RegisterValidators(serviceCollection, configurationModel, types);
    }

    private static void RegisterRequestProcessors(IServiceCollection serviceCollection, ConfigurationModel configurationModel, IEnumerable<Type> types)
    {
        var requestProcessors = types
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && (
                            i.GetGenericTypeDefinition() == typeof(IRequestProcessorAsync<,>) ||
                            i.GetGenericTypeDefinition() == typeof(IRequestProcessorAsync<>)
                ))
                .Select(i => new { Implementation = t, Interface = i }))
            .ToList();

        foreach (var rp in requestProcessors)
        {
            serviceCollection.TryAdd(new ServiceDescriptor(rp.Interface, rp.Implementation, configurationModel.Lifetime));
        }
    }
    
    private static void RegisterMessageProcessors(IServiceCollection serviceCollection, ConfigurationModel configurationModel, IEnumerable<Type> types)
    {
        var messageProcessors = types
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMessageProcessor<>))
                .Select(i => new { Implementation = t, Interface = i }))
            .ToList();

        foreach (var mp in messageProcessors)
        {
            serviceCollection.Add(new ServiceDescriptor(mp.Interface, mp.Implementation, configurationModel.Lifetime));
        }
    }

    private static void RegisterValidators(IServiceCollection serviceCollection, ConfigurationModel configurationModel, IEnumerable<Type> types)
    {
        var validators = types
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestValidator<>))
                .Select(i => new { Implementation = t, Interface = i }))
            .ToList();

        foreach (var validator in validators)
        {
            serviceCollection.TryAdd(new ServiceDescriptor(validator.Interface, validator.Implementation, configurationModel.Lifetime));
        }
    }
}
