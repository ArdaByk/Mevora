using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

namespace Mevora;

internal static class ConfigurationProcessor
{
    internal static void RegisterProcessors(ConfigurationModel configurationModel, IServiceCollection serviceCollection)
    {
        List<Assembly> assemblies = configurationModel.ProcessorsToBeRegistered;

        if (!assemblies.Any())
            throw new ArgumentException("No assemblies were registered. Use AddProcessorsFromAssembly or AddProcessorsFromAssemblies in AddMevora.");

        var types = assemblies.SelectMany(assembly => assembly.GetTypes()).Where(i => !i.IsInterface);

        var requestProcessors = types
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && (
                            i.GetGenericTypeDefinition() == typeof(IRequestProcessorAsync<,>) ||
                            i.GetGenericTypeDefinition() == typeof(IRequestProcessorAsync<>) ||
                            i.GetGenericTypeDefinition() == typeof(IMessageProcessor<>) ||
                            i.GetGenericTypeDefinition() == typeof(IRequestValidator<>)
                ))
                .Select(i => new { Implementation = t, Interface = i }))
            .ToList();

        foreach (var rp in requestProcessors)
        {
            if (rp.Interface.IsGenericType && rp.Interface.GetGenericTypeDefinition() == typeof(IMessageProcessor<>))
            {
                serviceCollection.Add(new ServiceDescriptor(rp.Interface, rp.Implementation, configurationModel.Lifetime));
            }
            else
            {
                serviceCollection.TryAdd(new ServiceDescriptor(rp.Interface, rp.Implementation, configurationModel.Lifetime));
            }
        }

        RegisterPipelineActions(configurationModel, serviceCollection, types);
    }

    private static void RegisterPipelineActions(ConfigurationModel configurationModel, IServiceCollection serviceCollection, IEnumerable<Type> types)
    {
        // Find all request types that implement IRequest or IRequest<T>
        var requestTypes = types
            .Where(t => !t.IsInterface && !t.IsAbstract)
            .Where(t => t.GetInterfaces().Any(i =>
                i == typeof(IRequest) ||
                (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>))))
            .ToList();

        foreach (var (serviceType, implementationType) in configurationModel.PipelineActions)
        {
            if (serviceType == typeof(IPipelineAction<,>))
            {
                foreach (var requestType in requestTypes)
                {
                    var requestInterfaces = requestType.GetInterfaces();

                    // Handle requests with response (IRequest<TResponse>)
                    var requestWithResponseInterface = requestInterfaces
                        .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>));

                    if (requestWithResponseInterface != null)
                    {
                        var responseType = requestWithResponseInterface.GetGenericArguments()[0];
                        var closedServiceType = typeof(IPipelineAction<,>).MakeGenericType(requestType, responseType);
                        var closedImplementationType = implementationType.MakeGenericType(requestType, responseType);

                        serviceCollection.TryAdd(new ServiceDescriptor(closedServiceType, closedImplementationType, configurationModel.Lifetime));
                    }
                    // Handle requests without response (IRequest) - use object as TResponse
                    else if (requestInterfaces.Contains(typeof(IRequest)))
                    {
                        var closedServiceType = typeof(IPipelineAction<,>).MakeGenericType(requestType, typeof(object));
                        var closedImplementationType = implementationType.MakeGenericType(requestType, typeof(object));

                        serviceCollection.TryAdd(new ServiceDescriptor(closedServiceType, closedImplementationType, configurationModel.Lifetime));
                    }
                }
            }
            else
            {
                serviceCollection.TryAdd(new ServiceDescriptor(serviceType, implementationType, configurationModel.Lifetime));
            }
        }
    }
}