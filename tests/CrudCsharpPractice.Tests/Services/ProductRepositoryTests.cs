using Microsoft.EntityFrameworkCore;
using CrudCsharpPractice.Api.Features.Products;
using CrudCsharpPractice.Api.Features.Products.Data;
using CrudCsharpPractice.Api.Features.Products.Services;

namespace CrudCsharpPractice.Tests.Services;

public class ProductRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly ProductRepository _repository;

    public ProductRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _repository = new ProductRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task AddAsync_ShouldCreateProduct_WithGeneratedIdAndTimestamps()
    {
        var product = new Product
        {
            Name = "Test Product",
            Description = "Test Description",
            Price = 99.99m,
            StockQuantity = 10
        };

        var result = await _repository.AddAsync(product);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.NotEqual(default, result.CreatedAt);
        Assert.NotEqual(default, result.UpdatedAt);
        Assert.Equal("Test Product", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WhenProductExists_ShouldReturnProduct()
    {
        var product = new Product
        {
            Name = "Existing Product",
            Description = "Description",
            Price = 50m,
            StockQuantity = 5
        };
        var created = await _repository.AddAsync(product);

        var result = await _repository.GetByIdAsync(created.Id);

        Assert.NotNull(result);
        Assert.Equal(created.Id, result.Id);
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
        await _repository.AddAsync(new Product { Name = "Product 1", Price = 10m, StockQuantity = 1 });
        await _repository.AddAsync(new Product { Name = "Product 2", Price = 20m, StockQuantity = 2 });
        await _repository.AddAsync(new Product { Name = "Product 3", Price = 30m, StockQuantity = 3 });

        var result = await _repository.GetAllAsync();

        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateProduct_AndSetUpdatedAt()
    {
        var product = await _repository.AddAsync(new Product
        {
            Name = "Original Name",
            Price = 10m,
            StockQuantity = 5
        });
        var originalUpdatedAt = product.UpdatedAt;

        await Task.Delay(10);
        product.Name = "Updated Name";
        var result = await _repository.UpdateAsync(product);

        Assert.Equal("Updated Name", result.Name);
        Assert.True(result.UpdatedAt >= originalUpdatedAt);
    }

    [Fact]
    public async Task DeleteAsync_WhenProductExists_ShouldReturnTrue_AndRemoveProduct()
    {
        var product = await _repository.AddAsync(new Product
        {
            Name = "To Delete",
            Price = 10m,
            StockQuantity = 1
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
