using Microsoft.EntityFrameworkCore;
using CrudCsharpPractice.Api.Features.Products;
using CrudCsharpPractice.Api.Features.Products.Data;
using CrudCsharpPractice.Api.Features.Shared.DependencyInjection;

namespace CrudCsharpPractice.Tests.Services;

public class ProductRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Repository<Product> _repository;

    public ProductRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _repository = new Repository<Product>(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task AddAsync_ShouldCreateProduct()
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Test Product",
            Description = "Test Description",
            Price = 99.99m,
            StockQuantity = 10,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await _repository.AddAsync(product);

        Assert.Equal("Test Product", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WhenProductExists_ShouldReturnProduct()
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Existing Product",
            Description = "Description",
            Price = 50m,
            StockQuantity = 5,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(product);

        var result = await _repository.GetByIdAsync(product.Id);

        Assert.NotNull(result);
        Assert.Equal(product.Id, result.Id);
        Assert.Equal("Existing Product", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WhenProductDoesNotExist_ShouldReturnNull()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllProducts()
    {
        await _repository.AddAsync(new Product { Id = Guid.NewGuid(), Name = "Product 1", Price = 10m, StockQuantity = 1, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await _repository.AddAsync(new Product { Id = Guid.NewGuid(), Name = "Product 2", Price = 20m, StockQuantity = 2, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await _repository.AddAsync(new Product { Id = Guid.NewGuid(), Name = "Product 3", Price = 30m, StockQuantity = 3, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });

        var result = await _repository.GetAllAsync();

        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateProduct()
    {
        var product = await _repository.AddAsync(new Product
        {
            Id = Guid.NewGuid(),
            Name = "Original Name",
            Price = 10m,
            StockQuantity = 5,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        product.Name = "Updated Name";
        var result = await _repository.UpdateAsync(product);

        Assert.Equal("Updated Name", result.Name);
    }

    [Fact]
    public async Task DeleteAsync_WhenProductExists_ShouldReturnTrue_AndRemoveProduct()
    {
        var product = await _repository.AddAsync(new Product
        {
            Id = Guid.NewGuid(),
            Name = "To Delete",
            Price = 10m,
            StockQuantity = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        var result = await _repository.DeleteAsync(product.Id);

        Assert.True(result);
        Assert.Null(await _repository.GetByIdAsync(product.Id));
    }

    [Fact]
    public async Task DeleteAsync_WhenProductDoesNotExist_ShouldReturnFalse()
    {
        var result = await _repository.DeleteAsync(Guid.NewGuid());

        Assert.False(result);
    }
}
