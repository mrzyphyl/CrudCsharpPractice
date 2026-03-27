using CrudCsharpPractice.Api.Features.Products.DTOs;
using CrudCsharpPractice.Api.Features.Products.Services;
using CrudCsharpPractice.Api.Features.Shared.Interfaces;
using CrudCsharpPractice.Api.Features.Shared.Middleware;

namespace CrudCsharpPractice.Api.Features.Products.Commands;

public class CreateProductCommand
{
    private readonly IRepository<Product> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductMessagePublisher _messagePublisher;

    public CreateProductCommand(IRepository<Product> repository, IUnitOfWork unitOfWork, IProductMessagePublisher messagePublisher)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _messagePublisher = messagePublisher;
    }

    public async Task<ProductDto> ExecuteAsync(CreateProductDto dto, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                StockQuantity = dto.StockQuantity
            };

            await _repository.AddAsync(product, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            await _messagePublisher.PublishProductCreatedAsync(product.Id, product.Name, cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return new ProductDto(product.Id, product.Name, product.Description, product.Price, product.StockQuantity);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw new ServiceUnavailableException(
                $"Failed to create product: {ex.Message}",
                "CREATE_PRODUCT_FAILED",
                ex.StackTrace);
        }
    }
}