using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;

namespace CrudCsharpPractice.Api.Features.Shared.Middleware;

public static class GlobalExceptionHandler
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app, IHostEnvironment environment)
    {
        app.UseExceptionHandler(new ExceptionHandlerOptions
        {
            ExceptionHandler = async context =>
            {
                var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
                var exception = exceptionFeature?.Error;

                var (statusCode, errorResponse) = exception switch
                {
                    NotFoundException ex => (HttpStatusCode.NotFound, new ErrorResponse
                    {
                        TraceId = context.TraceIdentifier,
                        Message = ex.Message,
                        Code = ex.Code,
                        Details = ex.Details,
                        Timestamp = DateTime.UtcNow
                    }),
                    ValidationException ex => (HttpStatusCode.BadRequest, new ErrorResponse
                    {
                        TraceId = context.TraceIdentifier,
                        Message = ex.Message,
                        Code = ex.Code,
                        Errors = ex.Errors,
                        Timestamp = DateTime.UtcNow
                    }),
                    ConflictException ex => (HttpStatusCode.Conflict, new ErrorResponse
                    {
                        TraceId = context.TraceIdentifier,
                        Message = ex.Message,
                        Code = ex.Code,
                        Details = ex.Details,
                        Timestamp = DateTime.UtcNow
                    }),
                    UnauthorizedException ex => (HttpStatusCode.Unauthorized, new ErrorResponse
                    {
                        TraceId = context.TraceIdentifier,
                        Message = ex.Message,
                        Code = ex.Code,
                        Timestamp = DateTime.UtcNow
                    }),
                    ServiceUnavailableException ex => (HttpStatusCode.ServiceUnavailable, new ErrorResponse
                    {
                        TraceId = context.TraceIdentifier,
                        Message = ex.Message,
                        Code = ex.Code,
                        Details = ex.Details,
                        RetryAfter = ex.RetryAfter,
                        Timestamp = DateTime.UtcNow
                    }),
                    _ => (HttpStatusCode.InternalServerError, new ErrorResponse
                    {
                        TraceId = context.TraceIdentifier,
                        Message = environment.IsDevelopment() ? (exception?.Message ?? "An unexpected error occurred") : "An unexpected error occurred",
                        Code = "INTERNAL_ERROR",
                        Details = environment.IsDevelopment() ? (exception?.StackTrace ?? string.Empty) : null,
                        Timestamp = DateTime.UtcNow
                    })
                };

                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)statusCode;

                if (errorResponse.RetryAfter.HasValue)
                {
                    context.Response.Headers.RetryAfter = errorResponse.RetryAfter.Value.ToString();
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = environment.IsDevelopment()
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, options));
            }
        });

        return app;
    }
}

public class ErrorResponse
{
    public string TraceId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Details { get; set; }
    public List<ValidationError>? Errors { get; set; }
    public int? RetryAfter { get; set; }
    public DateTime Timestamp { get; set; }
}

public class ValidationError
{
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class NotFoundException : Exception
{
    public string Code { get; }
    public string? Details { get; }

    public NotFoundException(string message, string code = "NOT_FOUND", string? details = null)
        : base(message)
    {
        Code = code;
        Details = details;
    }
}

public class ValidationException : Exception
{
    public string Code { get; }
    public List<ValidationError> Errors { get; }

    public ValidationException(string message, string code = "VALIDATION_ERROR", List<ValidationError>? errors = null)
        : base(message)
    {
        Code = code;
        Errors = errors ?? new List<ValidationError>();
    }
}

public class ConflictException : Exception
{
    public string Code { get; }
    public string? Details { get; }

    public ConflictException(string message, string code = "CONFLICT", string? details = null)
        : base(message)
    {
        Code = code;
        Details = details;
    }
}

public class UnauthorizedException : Exception
{
    public string Code { get; }

    public UnauthorizedException(string message, string code = "UNAUTHORIZED")
        : base(message)
    {
        Code = code;
    }
}

public class ServiceUnavailableException : Exception
{
    public string Code { get; }
    public string? Details { get; }
    public int RetryAfter { get; }

    public ServiceUnavailableException(string message, string code = "SERVICE_UNAVAILABLE", string? details = null, int retryAfter = 30)
        : base(message)
    {
        Code = code;
        Details = details;
        RetryAfter = retryAfter;
    }
}
