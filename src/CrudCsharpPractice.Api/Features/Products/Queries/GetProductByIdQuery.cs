using CrudCsharpPractice.Api.Features.Products.DTOs;
using CrudCsharpPractice.Api.Features.Products.Services;

namespace CrudCsharpPractice.Api.Features.Products.Queries;

public class GetProductByIdQuery
{
    private readonly IProductRepository _repository;

    public GetProductByIdQuery(IProductRepository repository)
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
