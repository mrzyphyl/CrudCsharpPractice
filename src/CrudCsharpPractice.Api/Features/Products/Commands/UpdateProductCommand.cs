using CrudCsharpPractice.Api.Features.Products.DTOs;
using CrudCsharpPractice.Api.Features.Products.Services;
using CrudCsharpPractice.Api.Features.Shared.Interfaces;
using CrudCsharpPractice.Api.Features.Shared.Middleware;

namespace CrudCsharpPractice.Api.Features.Products.Commands;

public class UpdateProductCommand
{
    private readonly IRepository<Product> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductMessagePublisher _messagePublisher;

    public UpdateProductCommand(IRepository<Product> repository, IUnitOfWork unitOfWork, IProductMessagePublisher messagePublisher)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _messagePublisher = messagePublisher;
    }

    public async Task<ProductDto?> ExecuteAsync(UpdateProductDto dto, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var product = await _repository.GetByIdAsync(dto.Id, cancellationToken);
            if (product == null) return null;

            product.Name = dto.Name;
            product.Description = dto.Description;
            product.Price = dto.Price;
            product.StockQuantity = dto.StockQuantity;

            await _repository.UpdateAsync(product, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            await _messagePublisher.PublishProductUpdatedAsync(product.Id, product.Name, cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return new ProductDto(product.Id, product.Name, product.Description, product.Price, product.StockQuantity);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw new ServiceUnavailableException(
                $"Failed to update product: {ex.Message}",
                "UPDATE_PRODUCT_FAILED",
                ex.StackTrace);
        }
    }
}