using CrudCsharpPractice.Api.Features.Products.Services;
using CrudCsharpPractice.Api.Features.Shared.DependencyInjection;

namespace CrudCsharpPractice.Api.Features.Products.Commands;

public class DeleteProductCommand
{
    private readonly IRepository<Product> _repository;
    private readonly IProductMessagePublisher _messagePublisher;

    public DeleteProductCommand(IRepository<Product> repository, IProductMessagePublisher messagePublisher)
    {
        _repository = repository;
        _messagePublisher = messagePublisher;
    }

    public async Task<bool> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var deleted = await _repository.DeleteAsync(id, cancellationToken);
        
        if (deleted)
        {
            await _messagePublisher.PublishProductDeletedAsync(id, cancellationToken);
        }

        return deleted;
    }
}
