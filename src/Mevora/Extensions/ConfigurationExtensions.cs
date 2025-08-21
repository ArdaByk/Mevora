using Mevora;

namespace Microsoft.Extensions.DependencyInjection;

public static class ConfigurationExtensions
{
    public static IServiceCollection AddMevora(this IServiceCollection services, Action<ConfigurationModel> configurations)
    {
        var config = new ConfigurationModel();
        configurations(config);

        ConfigurationProcessor.RegisterProcessors(config, services);

        services.AddSingleton<IMevoraDispatcher>(provider =>
        {
            return new MevoraDispatcher(provider);
        });

        return services;
    }
}
