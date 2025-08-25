using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mevora.Abstractions.Configuration;

namespace Mevora.Core.Configuration;

internal class PipelineRegistrar : IServiceRegistrar
{
    public void RegisterServices(IServiceCollection serviceCollection, ConfigurationModel configurationModel)
    {
        var assemblies = configurationModel.ProcessorsToBeRegistered;
        var types = assemblies.SelectMany(assembly => assembly.GetTypes()).Where(i => !i.IsInterface);
        
        RegisterPipelineActions(configurationModel, serviceCollection, types);
    }

    private static void RegisterPipelineActions(ConfigurationModel configurationModel, IServiceCollection serviceCollection, IEnumerable<Type> types)
    {
        var requestTypes = GetRequestTypes(types);

        foreach (var (serviceType, implementationType) in configurationModel.PipelineActions)
        {
            if (serviceType == typeof(IPipelineAction<,>))
            {
                RegisterGenericPipelineActions(serviceCollection, configurationModel, requestTypes, implementationType);
            }
            else
            {
                serviceCollection.TryAdd(new ServiceDescriptor(serviceType, implementationType, configurationModel.Lifetime));
            }
        }
    }

    private static List<Type> GetRequestTypes(IEnumerable<Type> types)
    {
        return types
            .Where(t => !t.IsInterface && !t.IsAbstract)
            .Where(t => t.GetInterfaces().Any(i =>
                i == typeof(IRequest) ||
                (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>))))
            .ToList();
    }

    private static void RegisterGenericPipelineActions(IServiceCollection serviceCollection, ConfigurationModel configurationModel, List<Type> requestTypes, Type implementationType)
    {
        foreach (var requestType in requestTypes)
        {
            var requestInterfaces = requestType.GetInterfaces();

            var requestWithResponseInterface = requestInterfaces
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>));

            if (requestWithResponseInterface != null)
            {
                var responseType = requestWithResponseInterface.GetGenericArguments()[0];
                var closedServiceType = typeof(IPipelineAction<,>).MakeGenericType(requestType, responseType);
                var closedImplementationType = implementationType.MakeGenericType(requestType, responseType);

                serviceCollection.TryAdd(new ServiceDescriptor(closedServiceType, closedImplementationType, configurationModel.Lifetime));
            }
            else if (requestInterfaces.Contains(typeof(IRequest)))
            {
                var closedServiceType = typeof(IPipelineAction<,>).MakeGenericType(requestType, typeof(object));
                var closedImplementationType = implementationType.MakeGenericType(requestType, typeof(object));

                serviceCollection.TryAdd(new ServiceDescriptor(closedServiceType, closedImplementationType, configurationModel.Lifetime));
            }
        }
    }
}
