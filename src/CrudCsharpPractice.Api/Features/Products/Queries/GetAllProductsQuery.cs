using CrudCsharpPractice.Api.Features.Products.DTOs;
using CrudCsharpPractice.Api.Features.Products.Services;

namespace CrudCsharpPractice.Api.Features.Products.Queries;

public class GetAllProductsQuery
{
    private readonly IProductRepository _repository;

    public GetAllProductsQuery(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<ProductDto>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var products = await _repository.GetAllAsync(cancellationToken);
        
        return products.Select(p => new ProductDto(p.Id, p.Name, p.Description, p.Price, p.StockQuantity));
    }
}
