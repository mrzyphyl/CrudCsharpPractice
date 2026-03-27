using System.Reflection;
using CrudCsharpPractice.Api.Features.Products.Data;
using Microsoft.EntityFrameworkCore;

namespace CrudCsharpPractice.Api.Features.Shared.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase("CrudCsharpPracticeDb"));

        return services;
    }

    public static IServiceCollection AddCaching(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis") ?? "localhost:6379";
            options.InstanceName = "CrudCsharpPractice:";
        });

        return services;
    }

    public static IServiceCollection AddProductServices(this IServiceCollection services, Assembly assembly)
    {
        services.AddServicesFromAttributes(assembly);
        return services;
    }
}