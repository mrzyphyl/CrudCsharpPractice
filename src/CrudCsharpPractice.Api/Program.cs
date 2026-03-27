using System.Reflection;
using CrudCsharpPractice.Api.Features.Shared.Configuration;
using CrudCsharpPractice.Api.Features.Shared.DependencyInjection;
using CrudCsharpPractice.Api.Features.Shared.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services
    .AddDatabase(builder.Configuration)
    .AddCaching(builder.Configuration)
    .AddServicesFromAttributes(Assembly.GetExecutingAssembly());

builder.Services
    .AddRateLimiting()
    .AddHealthChecks(builder.Configuration);

var app = builder.Build();

app.UseGlobalExceptionHandler(app.Environment);
app.UseRateLimiter();

app.MapControllers();

app.MapGet("/health/ready", () => Results.Ok(new { status = "ready", timestamp = DateTime.UtcNow }))
    .WithTags("health");

app.MapGet("/health/live", () => Results.Ok(new { status = "alive", timestamp = DateTime.UtcNow }))
    .ExcludeFromDescription();

var port = builder.Configuration.GetValue<int>("AppSettings:Port", 8080);
app.Urls.Clear();
app.Urls.Add($"http://0.0.0.0:{port}");

app.Run();
