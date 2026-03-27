using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using CrudCsharpPractice.Api.Features.Products;
using CrudCsharpPractice.Api.Features.Products.Controllers;
using CrudCsharpPractice.Api.Features.Products.DTOs;
using CrudCsharpPractice.Api.Features.Products.Services;
using CrudCsharpPractice.Api.Features.Shared.Messaging;
using CrudCsharpPractice.Api.Features.Shared.Middleware;

namespace CrudCsharpPractice.Tests.Controllers;

public class ProductsControllerTests
{
    private readonly Mock<IProductRepository> _repositoryMock;
    private readonly Mock<IProductMessagePublisher> _publisherMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly Mock<IRabbitMqService> _rabbitMqMock;
    private readonly Mock<ILogger<ProductsController>> _loggerMock;
    private readonly ProductsController _controller;

    public ProductsControllerTests()
    {
        _repositoryMock = new Mock<IProductRepository>();
        _publisherMock = new Mock<IProductMessagePublisher>();
        _cacheMock = new Mock<ICacheService>();
        _rabbitMqMock = new Mock<IRabbitMqService>();
        _loggerMock = new Mock<ILogger<ProductsController>>();

        _controller = new ProductsController(
            _repositoryMock.Object,
            _publisherMock.Object,
            _cacheMock.Object,
            _rabbitMqMock.Object,
            _loggerMock.Object);

        _cacheMock.Setup(c => c.GetAsync<IEnumerable<ProductDto>>(It.IsAny<string>(), default))
            .ReturnsAsync((IEnumerable<ProductDto>?)null);
        _cacheMock.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<IEnumerable<ProductDto>>(), It.IsAny<TimeSpan?>(), default))
            .Returns(Task.CompletedTask);
        _cacheMock.Setup(c => c.GetAsync<ProductDto>(It.IsAny<string>(), default))
            .ReturnsAsync((ProductDto?)null);
        _cacheMock.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<ProductDto>(), It.IsAny<TimeSpan?>(), default))
            .Returns(Task.CompletedTask);
        _rabbitMqMock.Setup(r => r.PublishMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), default))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task GetAll_WhenNoCache_ShouldReturnProducts_FromRepository()
    {
        var products = new List<ProductDto>
        {
            new(Guid.NewGuid(), "Product 1", "Desc 1", 10m, 5),
            new(Guid.NewGuid(), "Product 2", "Desc 2", 20m, 10)
        };
        _repositoryMock.Setup(r => r.GetAllAsync(default)).ReturnsAsync(products.Select(p => new Product
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price,
            StockQuantity = p.StockQuantity
        }));

        var result = await _controller.GetAll(default);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<InfoResponse<IEnumerable<ProductDto>>>(okResult.Value);
        Assert.Equal(2, response.Data?.Count());
        Assert.Equal("SUCCESS", response.Code);
    }

    [Fact]
    public async Task GetAll_WhenCacheHit_ShouldReturnCachedProducts()
    {
        var cachedProducts = new List<ProductDto>
        {
            new(Guid.NewGuid(), "Cached Product", "Desc", 10m, 5)
        };
        _cacheMock.Setup(c => c.GetAsync<IEnumerable<ProductDto>>(It.IsAny<string>(), default))
            .ReturnsAsync(cachedProducts);

        var result = await _controller.GetAll(default);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<InfoResponse<IEnumerable<ProductDto>>>(okResult.Value);
        Assert.Equal("Products retrieved from cache", response.Message);
        _repositoryMock.Verify(r => r.GetAllAsync(default), Times.Never);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ShouldThrowNotFoundException()
    {
        var productId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync((Product?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _controller.GetById(productId, default));
    }

    [Fact]
    public async Task Create_WithValidData_ShouldReturnCreated()
    {
        var dto = new CreateProductDto("New Product", "Description", 99.99m, 10);
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
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Product>(), default)).ReturnsAsync(createdProduct);

        var result = await _controller.Create(dto, default);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<InfoResponse<ProductDto>>(createdResult.Value);
        Assert.Equal("CREATED", response.Code);
        _rabbitMqMock.Verify(r => r.PublishMessageAsync("cache.invalidation", "product.created", It.IsAny<object>(), default), Times.Once);
    }

    [Fact]
    public async Task Create_WithEmptyName_ShouldThrowValidationException()
    {
        var dto = new CreateProductDto("", "Description", 99.99m, 10);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _controller.Create(dto, default));
        Assert.Equal("VALIDATION_ERROR", exception.Code);
        Assert.Contains(exception.Errors, e => e.Field == "name");
    }

    [Fact]
    public async Task Create_WithNegativePrice_ShouldThrowValidationException()
    {
        var dto = new CreateProductDto("Valid Name", "Description", -10m, 10);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _controller.Create(dto, default));
        Assert.Contains(exception.Errors, e => e.Field == "price");
    }

    [Fact]
    public async Task Update_WhenIdMismatch_ShouldThrowValidationException()
    {
        var dto = new UpdateProductDto(Guid.NewGuid(), "Name", "Desc", 10m, 5);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _controller.Update(Guid.NewGuid(), dto, default));
        Assert.Equal("ID_MISMATCH", exception.Code);
    }

    [Fact]
    public async Task Delete_WhenNotFound_ShouldThrowNotFoundException()
    {
        var productId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.DeleteAsync(productId, default)).ReturnsAsync(false);

        await Assert.ThrowsAsync<NotFoundException>(() => _controller.Delete(productId, default));
    }

    [Fact]
    public async Task Delete_WhenSuccessful_ShouldReturnDeleted_AndInvalidateCache()
    {
        var productId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.DeleteAsync(productId, default)).ReturnsAsync(true);

        var result = await _controller.Delete(productId, default);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<InfoResponse<object>>(okResult.Value);
        Assert.Equal("DELETED", response.Code);
        _rabbitMqMock.Verify(r => r.PublishMessageAsync("cache.invalidation", "product.deleted", It.IsAny<object>(), default), Times.Once);
    }
}
