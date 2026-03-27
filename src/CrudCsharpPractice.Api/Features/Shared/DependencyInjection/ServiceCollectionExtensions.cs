using CrudCsharpPractice.Api.Features.Products.Data;
using CrudCsharpPractice.Api.Features.Products.Services;
using CrudCsharpPractice.Api.Features.Shared.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

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

        services.AddSingleton<ICacheService, RedisCacheService>();

        return services;
    }

    public static IServiceCollection AddMessaging(this IServiceCollection services)
    {
        services.AddSingleton<IRabbitMqService, RabbitMqService>();
        services.AddHostedService<CacheInvalidationConsumer>();

        return services;
    }

    public static IServiceCollection AddProductServices(this IServiceCollection services)
    {
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductMessagePublisher, ProductMessagePublisher>();

        return services;
    }
}
