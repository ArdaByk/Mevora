using Mevora;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection;

public class ConfigurationModel
{
    internal List<Assembly> ProcessorsToBeRegistered { get; } = new();
    internal ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Transient;
    internal List<(Type ServiceType, Type ImplementationType)> PipelineActions { get; } = new();

    public ConfigurationModel AddProcessorsFromAssembly(Assembly assembly)
    {
        ProcessorsToBeRegistered.Add(assembly);
        return this;
    }

    public ConfigurationModel AddProcessorsFromAssemblies(List<Assembly> assemblies)
    {
        ProcessorsToBeRegistered.AddRange(assemblies);
        return this;
    }

    public ConfigurationModel WithServiceLifetime(ServiceLifetime serviceLifetime)
    {
        Lifetime = serviceLifetime;
        return this;
    }

    public ConfigurationModel AddPipelineAction(Type pipelineActionType)
    {
        if (!pipelineActionType.IsGenericTypeDefinition)
            throw new ArgumentException("Pipeline Action type must be an open generic type", nameof(pipelineActionType));

        var genericArgCount = pipelineActionType.GetGenericArguments().Length;

        if (genericArgCount == 2)
        {
            PipelineActions.Add((typeof(IPipelineAction<,>), pipelineActionType));
        }
        else if (genericArgCount == 1)
        {
            PipelineActions.Add((typeof(IPipelineAction<>), pipelineActionType));
        }
        else
        {
            throw new ArgumentException("Pipeline Action must have 1 or 2 generic parameters", nameof(pipelineActionType));
        }

        return this;
    }
}