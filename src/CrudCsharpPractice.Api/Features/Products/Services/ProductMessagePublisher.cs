using CrudCsharpPractice.Api.Features.Shared.Messaging;

namespace CrudCsharpPractice.Api.Features.Products.Services;

public interface IProductMessagePublisher
{
    Task PublishProductCreatedAsync(Guid productId, string productName, CancellationToken cancellationToken = default);
    Task PublishProductUpdatedAsync(Guid productId, string productName, CancellationToken cancellationToken = default);
    Task PublishProductDeletedAsync(Guid productId, CancellationToken cancellationToken = default);
}

public class ProductMessagePublisher : IProductMessagePublisher
{
    private readonly IRabbitMqService _rabbitMqService;

    public ProductMessagePublisher(IRabbitMqService rabbitMqService)
    {
        _rabbitMqService = rabbitMqService;
    }

    public Task PublishProductCreatedAsync(Guid productId, string productName, CancellationToken cancellationToken = default)
    {
        var message = new { EventType = "ProductCreated", ProductId = productId, ProductName = productName, Timestamp = DateTime.UtcNow };
        return _rabbitMqService.PublishMessageAsync("product.events", "product.created", message, cancellationToken);
    }

    public Task PublishProductUpdatedAsync(Guid productId, string productName, CancellationToken cancellationToken = default)
    {
        var message = new { EventType = "ProductUpdated", ProductId = productId, ProductName = productName, Timestamp = DateTime.UtcNow };
        return _rabbitMqService.PublishMessageAsync("product.events", "product.updated", message, cancellationToken);
    }

    public Task PublishProductDeletedAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var message = new { EventType = "ProductDeleted", ProductId = productId, Timestamp = DateTime.UtcNow };
        return _rabbitMqService.PublishMessageAsync("product.events", "product.deleted", message, cancellationToken);
    }
}
