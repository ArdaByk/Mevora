using Mevora;

namespace Microsoft.Extensions.DependencyInjection;

public static class ConfigurationExtensions
{
    public static IServiceCollection AddMevora(this IServiceCollection services, Action<ConfigurationModel> configurations)
    {
        var config = new ConfigurationModel();
        configurations(config);

        ConfigurationProcessor.RegisterProcessors(config, services);

        return services;
    }
}
