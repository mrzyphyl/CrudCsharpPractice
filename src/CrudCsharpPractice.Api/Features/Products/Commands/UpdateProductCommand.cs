using CrudCsharpPractice.Api.Features.Products.DTOs;
using CrudCsharpPractice.Api.Features.Products.Services;
using CrudCsharpPractice.Api.Features.Shared.DependencyInjection;

namespace CrudCsharpPractice.Api.Features.Products.Commands;

public class UpdateProductCommand
{
    private readonly IRepository<Product> _repository;
    private readonly IProductMessagePublisher _messagePublisher;

    public UpdateProductCommand(IRepository<Product> repository, IProductMessagePublisher messagePublisher)
    {
        _repository = repository;
        _messagePublisher = messagePublisher;
    }

    public async Task<ProductDto?> ExecuteAsync(UpdateProductDto dto, CancellationToken cancellationToken = default)
    {
        var product = await _repository.GetByIdAsync(dto.Id, cancellationToken);
        if (product == null) return null;

        product.Name = dto.Name;
        product.Description = dto.Description;
        product.Price = dto.Price;
        product.StockQuantity = dto.StockQuantity;

        var updated = await _repository.UpdateAsync(product, cancellationToken);
        
        await _messagePublisher.PublishProductUpdatedAsync(updated.Id, updated.Name, cancellationToken);

        return new ProductDto(updated.Id, updated.Name, updated.Description, updated.Price, updated.StockQuantity);
    }
}
