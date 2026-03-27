namespace CrudCsharpPractice.Api.Features.Products.DTOs;

public record ProductDto(Guid Id, string Name, string Description, decimal Price, int StockQuantity);

public record CreateProductDto(string Name, string Description, decimal Price, int StockQuantity);

public record UpdateProductDto(Guid Id, string Name, string Description, decimal Price, int StockQuantity);
