using CrudCsharpPractice.Api.Features.Products.Data;
using CrudCsharpPractice.Api.Features.Shared.DependencyInjection;
using CrudCsharpPractice.Api.Features.Shared.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace CrudCsharpPractice.Api.Features.Shared.Interfaces;

[Scoped]
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public bool HasActiveTransaction => _transaction != null;

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null) return;

        try
        {
            await _context.Database.CommitTransactionAsync(cancellationToken);
        }
        finally
        {
            _transaction?.Dispose();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null) return;

        try
        {
            await _transaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            _transaction?.Dispose();
            _transaction = null;
        }
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        _transaction?.Dispose();
    }
}