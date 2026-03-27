using CrudCsharpPractice.Api.Features.Products.DTOs;
using CrudCsharpPractice.Api.Features.Products.Services;
using CrudCsharpPractice.Api.Features.Shared.DependencyInjection;

namespace CrudCsharpPractice.Api.Features.Products.Commands;

public class CreateProductCommand
{
    private readonly IRepository<Product> _repository;
    private readonly IProductMessagePublisher _messagePublisher;

    public CreateProductCommand(IRepository<Product> repository, IProductMessagePublisher messagePublisher)
    {
        _repository = repository;
        _messagePublisher = messagePublisher;
    }

    public async Task<ProductDto> ExecuteAsync(CreateProductDto dto, CancellationToken cancellationToken = default)
    {
        var product = new Product
        {
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            StockQuantity = dto.StockQuantity
        };

        var created = await _repository.AddAsync(product, cancellationToken);
        
        await _messagePublisher.PublishProductCreatedAsync(created.Id, created.Name, cancellationToken);

        return new ProductDto(created.Id, created.Name, created.Description, created.Price, created.StockQuantity);
    }
}
