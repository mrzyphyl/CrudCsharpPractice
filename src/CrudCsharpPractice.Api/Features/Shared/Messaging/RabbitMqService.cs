using System.Text;
using System.Text.Json;
using CrudCsharpPractice.Api.Features.Shared.DependencyInjection;
using RabbitMQ.Client;

namespace CrudCsharpPractice.Api.Features.Shared.Messaging;

public interface IRabbitMqService : IAsyncDisposable
{
    Task PublishMessageAsync(string exchange, string routingKey, object message, CancellationToken cancellationToken = default);
}

[Singleton]
public class RabbitMqService : IRabbitMqService
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly ILogger<RabbitMqService> _logger;
    private bool _initialized;

    public IConnection Connection => _initialized ? _connection : null!;

    public RabbitMqService(IConfiguration configuration, ILogger<RabbitMqService> logger)
    {
        _logger = logger;
        
        var hostName = configuration.GetValue<string>("RabbitMq:HostName") ?? "localhost";
        var userName = configuration.GetValue<string>("RabbitMq:UserName") ?? "guest";
        var password = configuration.GetValue<string>("RabbitMq:Password") ?? "guest";
        var port = configuration.GetValue<int>("RabbitMq:Port", 5672);

        var factory = new ConnectionFactory
        {
            HostName = hostName,
            UserName = userName,
            Password = password,
            Port = port
        };

        try
        {
            _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
            _initialized = true;
            _logger.LogInformation("RabbitMQ connection established successfully");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to connect to RabbitMQ. Messaging will be disabled.");
            _initialized = false;
            _connection = null!;
            _channel = null!;
        }
    }

    public async Task PublishMessageAsync(string exchange, string routingKey, object message, CancellationToken cancellationToken = default)
    {
        if (!_initialized)
        {
            _logger.LogWarning("RabbitMQ not connected. Skipping message publish for {Exchange}/{RoutingKey}", exchange, routingKey);
            return;
        }

        try
        {
            await _channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true, cancellationToken: cancellationToken);

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json"
            };

            await _channel.BasicPublishAsync(exchange, routingKey, mandatory: false, properties, body, cancellationToken);
            
            _logger.LogInformation("Published message to {Exchange}/{RoutingKey}: {Message}", exchange, routingKey, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message to {Exchange}/{RoutingKey}", exchange, routingKey);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_initialized)
        {
            await _channel.CloseAsync();
            await _connection.CloseAsync();
        }
    }
}
