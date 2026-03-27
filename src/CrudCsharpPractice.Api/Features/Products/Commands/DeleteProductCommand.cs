using CrudCsharpPractice.Api.Features.Products.Services;
using CrudCsharpPractice.Api.Features.Shared.Interfaces;
using CrudCsharpPractice.Api.Features.Shared.Middleware;

namespace CrudCsharpPractice.Api.Features.Products.Commands;

public class DeleteProductCommand
{
    private readonly IRepository<Product> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductMessagePublisher _messagePublisher;

    public DeleteProductCommand(IRepository<Product> repository, IUnitOfWork unitOfWork, IProductMessagePublisher messagePublisher)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _messagePublisher = messagePublisher;
    }

    public async Task<bool> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var deleted = await _repository.DeleteAsync(id, cancellationToken);
            
            if (deleted)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _messagePublisher.PublishProductDeletedAsync(id, cancellationToken);
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return deleted;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw new ServiceUnavailableException(
                $"Failed to delete product: {ex.Message}",
                "DELETE_PRODUCT_FAILED",
                ex.StackTrace);
        }
    }
}