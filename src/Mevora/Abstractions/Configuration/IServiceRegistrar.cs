using Microsoft.Extensions.DependencyInjection;

namespace Mevora.Abstractions.Configuration;

public interface IServiceRegistrar
{
    void RegisterServices(IServiceCollection serviceCollection, ConfigurationModel configurationModel);
}
