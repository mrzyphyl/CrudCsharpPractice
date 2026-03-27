# CRUD C# Practice - Vertical Slice Architecture

A modern .NET 10 Web API implementing **Vertical Slice Architecture** with CQRS, Redis caching, RabbitMQ messaging, and production-ready features.

## Architecture Overview

```
crud-csharp-practice/
├── src/CrudCsharpPractice.Api/
│   ├── Features/
│   │   ├── Products/
│   │   │   ├── Commands/          # Write operations (Create, Update, Delete)
│   │   │   ├── Queries/          # Read operations (GetAll, GetById)
│   │   │   ├── Controllers/      # API endpoints
│   │   │   ├── DTOs/             # Data transfer objects
│   │   │   ├── Services/          # Repository & messaging
│   │   │   └── Data/              # DbContext
│   │   └── Shared/
│   │       ├── DependencyInjection/ # DI extensions
│   │       ├── Configuration/      # Rate limiting, health checks
│   │       ├── Messaging/          # RabbitMQ & Redis services
│   │       └── Middleware/         # Global error handling
│   └── Program.cs
└── tests/CrudCsharpPractice.Tests/
```

## Features

| Feature | Implementation |
|---------|---------------|
| **Architecture** | Vertical Slice + CQRS |
| **Database** | Entity Framework Core (InMemory/SQL Server) |
| **Caching** | Redis Distributed Cache |
| **Messaging** | RabbitMQ (Event-driven) |
| **Cache Invalidation** | Event-driven via RabbitMQ |
| **Rate Limiting** | 100 req/sec per IP |
| **Health Checks** | Self, Database, Redis |
| **Error Handling** | Global exception handler |
| **Testing** | xUnit + Moq (34 tests) |

## Request Flow

### Read Operations (Cache-Aside Pattern)
```
┌────────────────────────────────────────────────────────────────────┐
│  GET /api/products/123                                            │
│         │                                                         │
│         ▼                                                         │
│  ┌─────────────┐     HIT     ┌─────────┐                         │
│  │ Controller  │◄───────────►│  Redis  │ ◄── Return instantly   │
│  └─────────────┘             └─────────┘                         │
│         │                                                         │
│         │ MISS                                                     │
│         ▼                                                         │
│  ┌─────────────┐     READ    ┌─────────────┐                     │
│  │ Repository  │◄───────────│   Database  │                     │
│  └─────────────┘            └─────────────┘                     │
│         │                                                         │
│         │ Store in cache (30s TTL)                                │
│         ▼                                                         │
│  ┌─────────┐                                                      │
│  │  Redis  │                                                      │
│  └─────────┘                                                      │
└────────────────────────────────────────────────────────────────────┘
```

### Write Operations (Event-Driven Cache Invalidation)
```
┌────────────────────────────────────────────────────────────────────┐
│  POST /api/products                                                │
│         │                                                         │
│         ▼                                                         │
│  ┌─────────────┐     WRITE    ┌─────────────┐                     │
│  │ Controller  │────────────►│  Database   │                     │
│  └─────────────┘            └─────────────┘                     │
│         │                                                         │
│         │ Publish "product.created"                               │
│         ▼                                                         │
│  ┌─────────────────┐                                              │
│  │    RabbitMQ     │ ◄── All instances subscribe                 │
│  │ (cache.inv.)    │                                              │
│  └─────────────────┘                                              │
│         │                                                         │
│         │ For each instance:                                       │
│         ▼                                                         │
│  ┌─────────────┐     DELETE   ┌─────────┐                         │
│  │  Consumer   │────────────►│  Redis  │ ◄── Evict "products:*" │
│  └─────────────┘             └─────────┘                         │
└────────────────────────────────────────────────────────────────────┘
```

## API Endpoints

| Method | Endpoint | Description | Cache |
|--------|----------|-------------|-------|
| GET | `/api/products` | Get all products | 30s |
| GET | `/api/products/{id}` | Get product by ID | 30s |
| POST | `/api/products` | Create product | - |
| PUT | `/api/products/{id}` | Update product | - |
| DELETE | `/api/products/{id}` | Delete product | - |
| GET | `/health/ready` | Readiness probe | - |
| GET | `/health/live` | Liveness probe | - |

## Response Format

### Success Response
```json
{
  "data": { "id": "...", "name": "...", "price": 99.99 },
  "message": "Product retrieved successfully",
  "code": "SUCCESS",
  "timestamp": "2026-03-27T10:00:00Z"
}
```

### Error Response
```json
{
  "traceId": "0HN4ABC123...",
  "message": "Product with ID xxx not found",
  "code": "PRODUCT_NOT_FOUND",
  "details": null,
  "errors": null,
  "retryAfter": null,
  "timestamp": "2026-03-27T10:00:00Z"
}
```

## Configuration

### appsettings.json
```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  },
  "RabbitMq": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest"
  },
  "AppSettings": {
    "Port": 8080
  }
}
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Redis Server](https://redis.io/download)
- [RabbitMQ Server](https://www.rabbitmq.com/download.html)

## Running Locally

### 1. Start Infrastructure
```bash
# Redis
redis-server

# RabbitMQ
rabbitmq-server
```

### 2. Run the API
```bash
cd src/CrudCsharpPractice.Api
dotnet run
```

### 3. Run Tests
```bash
dotnet test
```

## Running with Docker

```bash
docker-compose up --build
```

## Load Balancing Setup

The project includes Nginx configuration for horizontal scaling:

```
                    ┌─────────────┐
                    │   Nginx     │
                    │ (Load Bal.) │
                    └──────┬──────┘
                           │
          ┌────────────────┼────────────────┐
          │                │                │
          ▼                ▼                ▼
    ┌──────────┐    ┌──────────┐    ┌──────────┐
    │  API-1   │    │  API-2   │    │  API-3   │
    │ :8080    │    │ :8081    │    │ :8082    │
    └────┬─────┘    └────┬─────┘    └────┬─────┘
         │                │                │
         └────────────────┼────────────────┘
                          │
              ┌───────────┴───────────┐
              ▼                       ▼
        ┌─────────┐            ┌─────────┐
        │  Redis  │            │RabbitMQ │
        └─────────┘            └─────────┘
```

## Project Structure by Feature

Each feature is self-contained with its own:
- **Commands/** - Write operations
- **Queries/** - Read operations
- **DTOs/** - Request/Response models
- **Services/** - Business logic
- **Controllers/** - HTTP endpoints

This is the **Vertical Slice** pattern - each feature is a complete slice of the application.

## Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Test Coverage
| Category | Tests |
|----------|-------|
| Repository CRUD | 6 tests |
| Commands | 6 tests |
| Controllers | 11 tests |
| Error Handling | 11 tests |

## Performance Considerations

### For Millions of Requests/Day

1. **Redis Cache** - Serves 80% of read requests instantly
2. **Event-Driven Invalidation** - All instances stay in sync
3. **Database Indexes** - Fast lookups on cache misses
4. **Rate Limiting** - Protects against abuse
5. **Horizontal Scaling** - Run multiple instances via Docker/Nginx

### Cache Key Patterns
```
product:{id}      → Single product
products:all      → All products list
```

## Custom Exceptions

| Exception | HTTP Status | Use Case |
|-----------|-------------|----------|
| `NotFoundException` | 404 | Resource not found |
| `ValidationException` | 400 | Input validation errors |
| `ConflictException` | 409 | Duplicate/conflict |
| `UnauthorizedException` | 401 | Auth failures |
| `ServiceUnavailableException` | 503 | External service down |

## License

MIT
