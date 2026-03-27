using CrudCsharpPractice.Api.Features.Products.Data;
using CrudCsharpPractice.Api.Features.Shared.DependencyInjection;
using CrudCsharpPractice.Api.Features.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CrudCsharpPractice.Api.Features.Shared.Interfaces;

[Scoped]
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;
    protected readonly IUnitOfWork _unitOfWork;

    public Repository(AppDbContext context, IUnitOfWork unitOfWork)
    {
        _context = context;
        _dbSet = context.Set<T>();
        _unitOfWork = unitOfWork;
    }

    public virtual async Task<T?> GetByIdAsync<TId>(TId id, CancellationToken cancellationToken = default) where TId : notnull
    {
        return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.ToListAsync(cancellationToken);
    }

    public virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
    }

    public virtual async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Update(entity);
    }

    public virtual async Task<bool> DeleteAsync<TId>(TId id, CancellationToken cancellationToken = default) where TId : notnull
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity == null) return false;

        _dbSet.Remove(entity);
        return true;
    }

    public virtual async Task<bool> ExistsAsync<TId>(TId id, CancellationToken cancellationToken = default) where TId : notnull
    {
        return await _dbSet.FindAsync(new object[] { id }, cancellationToken) != null;
    }
}