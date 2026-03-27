using CrudCsharpPractice.Api.Features.Products.Commands;
using CrudCsharpPractice.Api.Features.Products.DTOs;
using CrudCsharpPractice.Api.Features.Products.Queries;
using CrudCsharpPractice.Api.Features.Products.Services;
using CrudCsharpPractice.Api.Features.Shared.Interfaces;
using CrudCsharpPractice.Api.Features.Shared.Messaging;
using CrudCsharpPractice.Api.Features.Shared.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CrudCsharpPractice.Api.Features.Products.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly IRepository<Product> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductMessagePublisher _messagePublisher;
    private readonly ICacheService _cacheService;
    private readonly IRabbitMqService _rabbitMqService;
    private readonly ILogger<ProductsController> _logger;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

    public ProductsController(
        IRepository<Product> repository,
        IUnitOfWork unitOfWork,
        IProductMessagePublisher messagePublisher,
        ICacheService cacheService,
        IRabbitMqService rabbitMqService,
        ILogger<ProductsController> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _messagePublisher = messagePublisher;
        _cacheService = cacheService;
        _rabbitMqService = rabbitMqService;
        _logger = logger;
    }

    [HttpGet]
    [EnableRateLimiting("fixed")]
    [ProducesResponseType(typeof(InfoResponse<IEnumerable<ProductDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<InfoResponse<IEnumerable<ProductDto>>>> GetAll(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching all products");
        
        var cached = await _cacheService.GetAsync<IEnumerable<ProductDto>>(CacheKeys.AllProducts, cancellationToken);
        if (cached != null)
        {
            _logger.LogDebug("Returning cached products list");
            return Ok(InfoResponse<IEnumerable<ProductDto>>.Ok(cached, "Products retrieved from cache", "SUCCESS"));
        }
        
        var query = new GetAllProductsQuery(_repository);
        var products = await query.ExecuteAsync(cancellationToken);
        var productList = products.ToList();
        
        await _cacheService.SetAsync(CacheKeys.AllProducts, productList, CacheDuration, cancellationToken);
        
        _logger.LogInformation("Returning {Count} products from database", productList.Count);
        return Ok(InfoResponse<IEnumerable<ProductDto>>.Ok(productList, "Products retrieved successfully", "SUCCESS"));
    }

    [HttpGet("{id:guid}")]
    [EnableRateLimiting("fixed")]
    [ProducesResponseType(typeof(InfoResponse<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InfoResponse<ProductDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching product with ID: {ProductId}", id);
        
        var cached = await _cacheService.GetAsync<ProductDto>(CacheKeys.Product(id), cancellationToken);
        if (cached != null)
        {
            _logger.LogDebug("Returning cached product {ProductId}", id);
            return Ok(InfoResponse<ProductDto>.Ok(cached, "Product retrieved from cache", "SUCCESS"));
        }
        
        var query = new GetProductByIdQuery(_repository);
        var product = await query.ExecuteAsync(id, cancellationToken);
        
        if (product == null)
        {
            _logger.LogWarning("Product not found: {ProductId}", id);
            throw new NotFoundException($"Product with ID {id} not found", "PRODUCT_NOT_FOUND");
        }
        
        await _cacheService.SetAsync(CacheKeys.Product(id), product, CacheDuration, cancellationToken);
        
        return Ok(InfoResponse<ProductDto>.Ok(product, "Product retrieved successfully", "SUCCESS"));
    }

    [HttpPost]
    [EnableRateLimiting("fixed")]
    [ProducesResponseType(typeof(InfoResponse<ProductDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InfoResponse<ProductDto>>> Create([FromBody] CreateProductDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating new product: {ProductName}", dto.Name);
        
        ValidateCreateDto(dto);
        
        var command = new CreateProductCommand(_repository, _unitOfWork, _messagePublisher);
        var product = await command.ExecuteAsync(dto, cancellationToken);
        
        await InvalidateCacheAsync(product.Id, "created", cancellationToken);
        
        _logger.LogInformation("Product created successfully: {ProductId}", product.Id);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, 
            InfoResponse<ProductDto>.Created(product, "Product created successfully", "CREATED"));
    }

    [HttpPut("{id:guid}")]
    [EnableRateLimiting("fixed")]
    [ProducesResponseType(typeof(InfoResponse<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InfoResponse<ProductDto>>> Update(Guid id, [FromBody] UpdateProductDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating product with ID: {ProductId}", id);
        
        if (id != dto.Id)
        {
            throw new ValidationException("ID mismatch", "ID_MISMATCH", 
                new List<ValidationError> { new() { Field = "id", Message = "Route ID does not match body ID" } });
        }
        
        ValidateUpdateDto(dto);
        
        var command = new UpdateProductCommand(_repository, _unitOfWork, _messagePublisher);
        var product = await command.ExecuteAsync(dto, cancellationToken);
        
        if (product == null)
        {
            _logger.LogWarning("Product not found for update: {ProductId}", id);
            throw new NotFoundException($"Product with ID {id} not found", "PRODUCT_NOT_FOUND");
        }
        
        await InvalidateCacheAsync(id, "updated", cancellationToken);
        
        _logger.LogInformation("Product updated successfully: {ProductId}", id);
        return Ok(InfoResponse<ProductDto>.Ok(product, "Product updated successfully", "SUCCESS"));
    }

    [HttpDelete("{id:guid}")]
    [EnableRateLimiting("fixed")]
    [ProducesResponseType(typeof(InfoResponse<object>), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InfoResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting product with ID: {ProductId}", id);
        
        var command = new DeleteProductCommand(_repository, _unitOfWork, _messagePublisher);
        var deleted = await command.ExecuteAsync(id, cancellationToken);
        
        if (!deleted)
        {
            _logger.LogWarning("Product not found for deletion: {ProductId}", id);
            throw new NotFoundException($"Product with ID {id} not found", "PRODUCT_NOT_FOUND");
        }
        
        await InvalidateCacheAsync(id, "deleted", cancellationToken);
        
        _logger.LogInformation("Product deleted successfully: {ProductId}", id);
        return Ok(InfoResponse.Deleted("Product deleted successfully", "DELETED"));
    }

    private async Task InvalidateCacheAsync(Guid productId, string action, CancellationToken ct)
    {
        await _rabbitMqService.PublishMessageAsync("cache.invalidation", $"product.{action}", 
            new CacheInvalidationMessage { ProductId = productId, Action = action }, ct);
    }

    private static void ValidateCreateDto(CreateProductDto dto)
    {
        var errors = new List<ValidationError>();
        
        if (string.IsNullOrWhiteSpace(dto.Name))
            errors.Add(new ValidationError { Field = "name", Message = "Name is required" });
        else if (dto.Name.Length > 200)
            errors.Add(new ValidationError { Field = "name", Message = "Name must not exceed 200 characters" });
        
        if (dto.Price < 0)
            errors.Add(new ValidationError { Field = "price", Message = "Price cannot be negative" });
        
        if (dto.StockQuantity < 0)
            errors.Add(new ValidationError { Field = "stockQuantity", Message = "Stock quantity cannot be negative" });
        
        if (errors.Count > 0)
            throw new ValidationException("Validation failed", "VALIDATION_ERROR", errors);
    }

    private static void ValidateUpdateDto(UpdateProductDto dto)
    {
        var errors = new List<ValidationError>();
        
        if (string.IsNullOrWhiteSpace(dto.Name))
            errors.Add(new ValidationError { Field = "name", Message = "Name is required" });
        else if (dto.Name.Length > 200)
            errors.Add(new ValidationError { Field = "name", Message = "Name must not exceed 200 characters" });
        
        if (dto.Price < 0)
            errors.Add(new ValidationError { Field = "price", Message = "Price cannot be negative" });
        
        if (dto.StockQuantity < 0)
            errors.Add(new ValidationError { Field = "stockQuantity", Message = "Stock quantity cannot be negative" });
        
        if (errors.Count > 0)
            throw new ValidationException("Validation failed", "VALIDATION_ERROR", errors);
    }
}
