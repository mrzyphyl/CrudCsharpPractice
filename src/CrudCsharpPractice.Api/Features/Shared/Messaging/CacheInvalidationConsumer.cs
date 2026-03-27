using System.Text;
using System.Text.Json;
using CrudCsharpPractice.Api.Features.Shared.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CrudCsharpPractice.Api.Features.Shared.Messaging;

[Singleton]
public class CacheInvalidationConsumer : BackgroundService
{
    private readonly IConnection _connection;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CacheInvalidationConsumer> _logger;
    private IChannel? _channel;
    private const string Exchange = "cache.invalidation";
    private const string Queue = "cache.invalidation.api";

    public CacheInvalidationConsumer(
        IRabbitMqService rabbitMqService,
        ICacheService cacheService,
        ILogger<CacheInvalidationConsumer> logger)
    {
        _connection = ((RabbitMqService)rabbitMqService).Connection;
        _cacheService = cacheService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);
            
            await _channel.ExchangeDeclareAsync(Exchange, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);
            await _channel.QueueDeclareAsync(Queue, durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: stoppingToken);
            
            await _channel.QueueBindAsync(Queue, Exchange, "product.#", cancellationToken: stoppingToken);
            await _channel.QueueBindAsync(Queue, Exchange, "cache.#", cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var routingKey = ea.RoutingKey;
                    
                    _logger.LogInformation("Cache invalidation event received: {RoutingKey} - {Message}", routingKey, message);
                    
                    await ProcessInvalidationAsync(routingKey, message, stoppingToken);
                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process cache invalidation");
                    await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, cancellationToken: stoppingToken);
                }
            };

            await _channel.BasicConsumeAsync(Queue, autoAck: false, consumer, cancellationToken: stoppingToken);
            _logger.LogInformation("Cache invalidation consumer started on queue: {Queue}", Queue);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to start cache invalidation consumer. Cache events will be handled locally only.");
        }

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private Task ProcessInvalidationAsync(string routingKey, string message, CancellationToken ct)
    {
        var key = JsonSerializer.Deserialize<CacheInvalidationMessage>(message);
        if (key == null) return Task.CompletedTask;

        return routingKey switch
        {
            "product.updated" or "product.deleted" => InvalidateProductAndList(key.ProductId, ct),
            "product.created" => _cacheService.RemoveAsync(CacheKeys.AllProducts, ct),
            "cache.clear.all" => _cacheService.RemoveAsync(CacheKeys.AllProducts, ct),
            _ => Task.CompletedTask
        };
    }

    private async Task InvalidateProductAndList(Guid productId, CancellationToken ct)
    {
        await _cacheService.RemoveAsync(CacheKeys.Product(productId), ct);
        await _cacheService.RemoveAsync(CacheKeys.AllProducts, ct);
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        base.Dispose();
    }
}

public class CacheInvalidationMessage
{
    public Guid ProductId { get; set; }
    public string? Action { get; set; }
}

public static class CacheKeys
{
    public static string Product(Guid id) => $"product:{id}";
    public static string AllProducts => "products:all";
}
