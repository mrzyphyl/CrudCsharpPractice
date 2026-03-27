using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace CrudCsharpPractice.Api.Features.Shared.DependencyInjection;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddServicesFromAttributes(this IServiceCollection services, Assembly assembly)
    {
        var scopedTypes = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => t.GetCustomAttribute<ScopedAttribute>() != null)
            .ToList();

        var transientTypes = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => t.GetCustomAttribute<TransientAttribute>() != null)
            .ToList();

        var singletonTypes = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => t.GetCustomAttribute<SingletonAttribute>() != null)
            .ToList();

        RegisterServices(services, scopedTypes, ServiceLifetime.Scoped);
        RegisterServices(services, transientTypes, ServiceLifetime.Transient);
        RegisterServices(services, singletonTypes, ServiceLifetime.Singleton);

        return services;
    }

    private static void RegisterServices(IServiceCollection services, IEnumerable<Type> types, ServiceLifetime lifetime)
    {
        foreach (var implementationType in types)
        {
            var interfaceType = implementationType.GetInterfaces()
                .FirstOrDefault(i => i.Name == $"I{implementationType.Name}");

            if (interfaceType == null) continue;

            services.Add(new ServiceDescriptor(interfaceType, implementationType, lifetime));
        }
    }
}