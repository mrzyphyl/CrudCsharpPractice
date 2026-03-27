using CrudCsharpPractice.Api.Features.Products.Data;
using CrudCsharpPractice.Api.Features.Shared.DependencyInjection;
using CrudCsharpPractice.Api.Features.Shared.Middleware;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading.RateLimiting;

namespace CrudCsharpPractice.Api.Features.Shared.Configuration;

public static class ConfigurationExtensions
{
    public static IServiceCollection AddRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            
            options.AddPolicy("fixed", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromSeconds(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 10
                    }));
            
            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.ContentType = "application/json";
                var error = new ErrorResponse
                {
                    TraceId = context.HttpContext.TraceIdentifier,
                    Message = "Too many requests. Please try again later.",
                    Code = "RATE_LIMIT_EXCEEDED",
                    RetryAfter = 1,
                    Timestamp = DateTime.UtcNow
                };
                await context.HttpContext.Response.WriteAsJsonAsync(error, token);
            };
        });

        return services;
    }

    public static IServiceCollection AddHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "ready" })
            .AddDbContextCheck<AppDbContext>("db", tags: new[] { "ready" })
            .AddRedis(configuration.GetConnectionString("Redis") ?? "localhost:6379", name: "redis", tags: new[] { "ready" });

        return services;
    }
}
