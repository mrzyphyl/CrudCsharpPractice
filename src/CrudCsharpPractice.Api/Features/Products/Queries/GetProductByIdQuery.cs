using CrudCsharpPractice.Api.Features.Products.DTOs;
using CrudCsharpPractice.Api.Features.Shared.DependencyInjection;

namespace CrudCsharpPractice.Api.Features.Products.Queries;

public class GetProductByIdQuery
{
    private readonly IRepository<Product> _repository;

    public GetProductByIdQuery(IRepository<Product> repository)
    {
        _repository = repository;
    }

    public async Task<ProductDto?> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _repository.GetByIdAsync(id, cancellationToken);
        if (product == null) return null;

        return new ProductDto(product.Id, product.Name, product.Description, product.Price, product.StockQuantity);
    }
}
