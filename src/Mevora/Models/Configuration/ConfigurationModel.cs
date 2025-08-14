using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection;

public class ConfigurationModel
{
    internal List<Assembly> ProcessorsToBeRegistered { get; } = new();
    internal ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Transient;

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
}
