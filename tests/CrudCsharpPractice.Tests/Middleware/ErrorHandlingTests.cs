using CrudCsharpPractice.Api.Features.Shared.Middleware;

namespace CrudCsharpPractice.Tests.Middleware;

public class ErrorHandlingTests
{
    [Fact]
    public void NotFoundException_ShouldHaveCorrectProperties()
    {
        var exception = new NotFoundException("Resource not found", "CUSTOM_NOT_FOUND", "Additional details");

        Assert.Equal("Resource not found", exception.Message);
        Assert.Equal("CUSTOM_NOT_FOUND", exception.Code);
        Assert.Equal("Additional details", exception.Details);
    }

    [Fact]
    public void ValidationException_ShouldHaveErrorsList()
    {
        var errors = new List<ValidationError>
        {
            new() { Field = "name", Message = "Name is required" },
            new() { Field = "price", Message = "Price must be positive" }
        };
        var exception = new ValidationException("Validation failed", "VALIDATION_ERROR", errors);

        Assert.Equal("Validation failed", exception.Message);
        Assert.Equal(2, exception.Errors.Count);
        Assert.Equal("name", exception.Errors[0].Field);
    }

    [Fact]
    public void ValidationException_WithNullErrors_ShouldReturnEmptyList()
    {
        var exception = new ValidationException("Validation failed");

        Assert.Empty(exception.Errors);
    }

    [Fact]
    public void ConflictException_ShouldHaveCorrectProperties()
    {
        var exception = new ConflictException("Duplicate entry", "DUPLICATE", "Product with same name exists");

        Assert.Equal("Duplicate entry", exception.Message);
        Assert.Equal("DUPLICATE", exception.Code);
        Assert.Equal("Product with same name exists", exception.Details);
    }

    [Fact]
    public void UnauthorizedException_ShouldHaveCorrectProperties()
    {
        var exception = new UnauthorizedException("Invalid token", "INVALID_TOKEN");

        Assert.Equal("Invalid token", exception.Message);
        Assert.Equal("INVALID_TOKEN", exception.Code);
    }

    [Fact]
    public void ServiceUnavailableException_ShouldHaveRetryAfter()
    {
        var exception = new ServiceUnavailableException("Service down", "SERVICE_DOWN", "Redis unavailable", 60);

        Assert.Equal("Service down", exception.Message);
        Assert.Equal("SERVICE_DOWN", exception.Code);
        Assert.Equal(60, exception.RetryAfter);
    }

    [Fact]
    public void InfoResponse_Ok_ShouldHaveCorrectDefaults()
    {
        var response = InfoResponse<string>.Ok("test data", "Success", "SUCCESS");

        Assert.Equal("test data", response.Data);
        Assert.Equal("Success", response.Message);
        Assert.Equal("SUCCESS", response.Code);
        Assert.NotEqual(default, response.Timestamp);
    }

    [Fact]
    public void InfoResponse_Created_ShouldHaveCreatedCode()
    {
        var response = InfoResponse<string>.Created("data");

        Assert.Equal("CREATED", response.Code);
        Assert.Equal("Resource created", response.Message);
    }

    [Fact]
    public void InfoResponse_Deleted_ShouldHaveDeletedCode()
    {
        var response = InfoResponse<string>.Deleted();

        Assert.Equal("DELETED", response.Code);
        Assert.Equal("Resource deleted", response.Message);
    }

    [Fact]
    public void InfoResponse_WithMetadata_ShouldIncludeMetadata()
    {
        var metadata = new Dictionary<string, object>
        {
            { "totalCount", 100 },
            { "page", 1 }
        };
        var response = InfoResponse<string>.WithMetadata("data", metadata);

        Assert.NotNull(response.Metadata);
        Assert.Equal(100, response.Metadata["totalCount"]);
    }

    [Fact]
    public void ErrorResponse_ShouldHaveAllProperties()
    {
        var error = new ErrorResponse
        {
            TraceId = "trace-123",
            Message = "Error message",
            Code = "ERROR_CODE",
            Details = "Stack trace",
            RetryAfter = 30,
            Timestamp = DateTime.UtcNow
        };

        Assert.Equal("trace-123", error.TraceId);
        Assert.Equal("Error message", error.Message);
        Assert.Equal("ERROR_CODE", error.Code);
        Assert.Equal("Stack trace", error.Details);
        Assert.Equal(30, error.RetryAfter);
    }

    [Fact]
    public void ValidationError_ShouldHaveFieldAndMessage()
    {
        var error = new ValidationError
        {
            Field = "email",
            Message = "Invalid email format"
        };

        Assert.Equal("email", error.Field);
        Assert.Equal("Invalid email format", error.Message);
    }
}
