using Moq;
using CrudCsharpPractice.Api.Features.Products;
using CrudCsharpPractice.Api.Features.Products.Commands;
using CrudCsharpPractice.Api.Features.Products.DTOs;
using CrudCsharpPractice.Api.Features.Products.Services;
using CrudCsharpPractice.Api.Features.Shared.DependencyInjection;

namespace CrudCsharpPractice.Tests.Commands;

public class CreateProductCommandTests
{
    private readonly Mock<IRepository<Product>> _repositoryMock;
    private readonly Mock<IProductMessagePublisher> _publisherMock;
    private readonly CreateProductCommand _command;

    public CreateProductCommandTests()
    {
        _repositoryMock = new Mock<IRepository<Product>>();
        _publisherMock = new Mock<IProductMessagePublisher>();
        _command = new CreateProductCommand(_repositoryMock.Object, _publisherMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCreateProduct_AndPublishMessage()
    {
        var dto = new CreateProductDto("Test Product", "Description", 99.99m, 10);
        var createdProduct = new Product
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            StockQuantity = dto.StockQuantity,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Product>(), default))
            .ReturnsAsync(createdProduct);
        _publisherMock.Setup(p => p.PublishProductCreatedAsync(It.IsAny<Guid>(), It.IsAny<string>(), default))
            .Returns(Task.CompletedTask);

        var result = await _command.ExecuteAsync(dto);

        Assert.Equal(dto.Name, result.Name);
        Assert.Equal(dto.Price, result.Price);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Product>(), default), Times.Once);
        _publisherMock.Verify(p => p.PublishProductCreatedAsync(createdProduct.Id, createdProduct.Name, default), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRepositoryFails_ShouldNotPublishMessage()
    {
        var dto = new CreateProductDto("Test", "Desc", 10m, 1);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Product>(), default))
            .ThrowsAsync(new Exception("Database error"));

        await Assert.ThrowsAsync<Exception>(() => _command.ExecuteAsync(dto));
        _publisherMock.Verify(p => p.PublishProductCreatedAsync(It.IsAny<Guid>(), It.IsAny<string>(), default), Times.Never);
    }
}

public class UpdateProductCommandTests
{
    private readonly Mock<IRepository<Product>> _repositoryMock;
    private readonly Mock<IProductMessagePublisher> _publisherMock;
    private readonly UpdateProductCommand _command;

    public UpdateProductCommandTests()
    {
        _repositoryMock = new Mock<IRepository<Product>>();
        _publisherMock = new Mock<IProductMessagePublisher>();
        _command = new UpdateProductCommand(_repositoryMock.Object, _publisherMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProductExists_ShouldUpdate_AndPublishMessage()
    {
        var productId = Guid.NewGuid();
        var existingProduct = new Product
        {
            Id = productId,
            Name = "Original",
            Price = 10m,
            StockQuantity = 5
        };
        var dto = new UpdateProductDto(productId, "Updated", "New Desc", 20m, 10);
        var updatedProduct = new Product
        {
            Id = productId,
            Name = "Updated",
            Description = "New Desc",
            Price = 20m,
            StockQuantity = 10
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync(existingProduct);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Product>(), default)).ReturnsAsync(updatedProduct);
        _publisherMock.Setup(p => p.PublishProductUpdatedAsync(It.IsAny<Guid>(), It.IsAny<string>(), default))
            .Returns(Task.CompletedTask);

        var result = await _command.ExecuteAsync(dto);

        Assert.NotNull(result);
        Assert.Equal("Updated", result.Name);
        _publisherMock.Verify(p => p.PublishProductUpdatedAsync(productId, "Updated", default), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProductDoesNotExist_ShouldReturnNull()
    {
        var dto = new UpdateProductDto(Guid.NewGuid(), "Updated", "Desc", 20m, 10);
        _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Product?)null);

        var result = await _command.ExecuteAsync(dto);

        Assert.Null(result);
        _publisherMock.Verify(p => p.PublishProductUpdatedAsync(It.IsAny<Guid>(), It.IsAny<string>(), default), Times.Never);
    }
}

public class DeleteProductCommandTests
{
    private readonly Mock<IRepository<Product>> _repositoryMock;
    private readonly Mock<IProductMessagePublisher> _publisherMock;
    private readonly DeleteProductCommand _command;

    public DeleteProductCommandTests()
    {
        _repositoryMock = new Mock<IRepository<Product>>();
        _publisherMock = new Mock<IProductMessagePublisher>();
        _command = new DeleteProductCommand(_repositoryMock.Object, _publisherMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProductDeleted_ShouldPublishMessage()
    {
        var productId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.DeleteAsync(productId, default)).ReturnsAsync(true);
        _publisherMock.Setup(p => p.PublishProductDeletedAsync(productId, default))
            .Returns(Task.CompletedTask);

        var result = await _command.ExecuteAsync(productId);

        Assert.True(result);
        _publisherMock.Verify(p => p.PublishProductDeletedAsync(productId, default), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProductDoesNotExist_ShouldReturnFalse_AndNotPublish()
    {
        var productId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.DeleteAsync(productId, default)).ReturnsAsync(false);

        var result = await _command.ExecuteAsync(productId);

        Assert.False(result);
        _publisherMock.Verify(p => p.PublishProductDeletedAsync(It.IsAny<Guid>(), default), Times.Never);
    }
}
