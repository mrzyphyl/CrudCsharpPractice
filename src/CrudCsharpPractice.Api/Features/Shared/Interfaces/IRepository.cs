namespace CrudCsharpPractice.Api.Features.Shared.Interfaces;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync<TId>(TId id, CancellationToken cancellationToken = default) where TId : notnull;
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync<TId>(TId id, CancellationToken cancellationToken = default) where TId : notnull;
    Task<bool> ExistsAsync<TId>(TId id, CancellationToken cancellationToken = default) where TId : notnull;
}