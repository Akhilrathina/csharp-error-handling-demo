# C# Error Handling Demo: Exceptions vs Results with Problem Details

A comprehensive demonstration of different error handling approaches in .NET, showcasing traditional exceptions, the Result pattern, and RFC 7807 Problem Details implementation.

## Overview

This project demonstrates:
- **Exception-based error handling** (traditional .NET approach)
- **Result pattern** (functional programming approach)
- **Problem Details** (RFC 7807 standard for API error responses)
- **Third-party libraries** (OneOf, FluentResults, ErrorOr)
- **Performance comparisons** using BenchmarkDotNet

## Project Structure

```
csharp-error-handling-demo/
├── src/
│   ├── ErrorHandling.Domain/           # Core domain logic
│   │   ├── Entities/                   # Domain entities
│   │   ├── Exceptions/                 # Custom exception hierarchy
│   │   ├── Results/                    # Result pattern implementation
│   │   ├── Services/                   # Three service implementations
│   │   └── ValueObjects/               # Value objects with both approaches
│   │
│   ├── ErrorHandling.Api/              # Web API with Problem Details
│   │   ├── Controllers/                # Three controller approaches
│   │   ├── Middleware/                 # Global exception handling
│   │   └── ProblemDetails/             # Custom Problem Details factory
│   │
│   ├── ErrorHandling.Libraries/        # Third-party library examples
│   │   ├── OneOfExamples.cs           # OneOf discriminated unions
│   │   ├── FluentResultsExamples.cs   # FluentResults library
│   │   └── ErrorOrExamples.cs         # ErrorOr lightweight approach
│   │
│   └── ErrorHandling.Benchmarks/       # Performance comparisons
│       └── ExceptionVsResultBenchmark.cs
```

## Getting Started

### Prerequisites
- .NET 9.0 SDK or later
- Visual Studio 2022, VS Code, or Rider

### Running the API

```bash
cd src/ErrorHandling.Api
dotnet run
```

The API will start at `https://localhost:5001` with Swagger UI available at the root.

### API Endpoints

The API provides three versions of the same endpoints, each using a different error handling approach:

#### Exception-Based Endpoints (`/api/v1/exception/orders`)
- Traditional .NET approach
- Exceptions bubble up to global middleware
- Converted to Problem Details responses

#### Result-Based Endpoints (`/api/v1/result/orders`)
- Functional programming approach
- No exceptions for business logic
- Results converted to Problem Details

### Example API Calls

```bash
# Create order (Exception approach)
curl -X POST https://localhost:5001/api/v1/exception/orders \
  -H "Content-Type: application/json" \
  -d '{"customerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6", "shippingAddress": "123 Main St"}'

# Create order (Result approach)
curl -X POST https://localhost:5001/api/v1/result/orders \
  -H "Content-Type: application/json" \
  -d '{"customerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6", "shippingAddress": "123 Main St"}'
```

## Key Concepts Demonstrated

### 1. Domain Exceptions (RFC 7807 Compliant)

```csharp
public class BusinessRuleException : DomainException
{
    // Rich context with RFC 7807 properties
    public string Type { get; }
    public string Title { get; }
    public int Status { get; }
    public string Detail { get; }
    public string Instance { get; }
    // Additional metadata
    public string CorrelationId { get; }
    public IDictionary<string, object> Extensions { get; }
}
```

### 2. Result Pattern

```csharp
public Result<Order> CreateOrder(CreateOrderRequest request)
{
    if (!IsValid(request))
        return Result<Order>.Failure("Validation failed");
        
    // No exceptions thrown
    return Result<Order>.Success(new Order());
}
```

### 3. Problem Details Response

```json
{
  "type": "https://example.com/errors/insufficient-funds",
  "title": "Insufficient Funds",
  "status": 422,
  "detail": "Your account balance is $50.00, but the transaction requires $75.00",
  "instance": "/api/orders/123",
  "traceId": "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01",
  "timestamp": "2024-01-15T09:30:00Z",
  "balance": 50.00,
  "required": 75.00
}
```

## Running Benchmarks

```bash
cd src/ErrorHandling.Benchmarks
dotnet run -c Release
```

### Benchmark Results Preview

The benchmarks compare:
- Success path performance
- Failure path with shallow call stack
- Failure path with deep call stack
- Multiple validation scenarios
- Error propagation through stack

Typical results show:
- **Success path**: Results are slightly faster (no try-catch overhead)
- **Failure path**: Results are 50-100x faster (no stack unwinding)
- **Deep call stack**: Results maintain consistent performance
- **Memory allocation**: Results allocate less memory in failure scenarios

## Third-Party Libraries

### OneOf
Discriminated unions for C#:
```csharp
OneOf<User, ValidationError, NotFound> GetUser(Guid id);
```

### FluentResults
Rich result objects with metadata:
```csharp
Result.Ok(value)
    .WithSuccess("Operation completed")
    .WithMetadata("key", "value");
```

### ErrorOr
Lightweight and modern approach:
```csharp
ErrorOr<User> GetUser(Guid id)
{
    if (notFound)
        return Error.NotFound("User.NotFound", "User not found");
    return user;
}
```

## Best Practices

### When to Use Exceptions
- Infrastructure failures (network, database)
- Programming errors (null arguments)
- Guard clauses in constructors
- Third-party library integration

### When to Use Result Pattern
- Business rule violations
- Validation errors
- Domain operations that can fail
- When caller needs to handle different failures differently

## Decision Matrix

| Scenario | Exceptions | Result Pattern | Recommendation |
|----------|------------|----------------|----------------|
| Null arguments | ✅ Best | ❌ Overkill | Use exceptions |
| Network failures | ✅ Best | ❌ Awkward | Use exceptions |
| Validation errors | ❌ Expensive | ✅ Best | Use Result |
| Business rules | ❌ Not semantic | ✅ Best | Use Result |
| Multiple error types | ❌ Complex | ✅ Best | Use Result |
| Performance critical | ❌ Overhead | ✅ Best | Use Result |
| Deep call stacks | ❌ Slow unwinding | ✅ Fast | Use Result |

## Testing

To run tests (when implemented):
```bash
dotnet test
```

## Contributing

This is a demonstration project for educational purposes. Feel free to fork and experiment with different approaches.

## Resources

- [RFC 7807 - Problem Details for HTTP APIs](https://datatracker.ietf.org/doc/html/rfc7807)
- [OneOf Library](https://github.com/mcintyre321/OneOf)
- [FluentResults](https://github.com/altmann/FluentResults)
- [ErrorOr](https://github.com/amantinband/error-or)
- [Railway Oriented Programming](https://fsharpforfunandprofit.com/rop/)

## License

This project is for demonstration purposes and is provided as-is for educational use.