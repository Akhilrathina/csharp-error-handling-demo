using OneOf;
using OneOf.Types;

namespace ErrorHandling.Libraries;

/// <summary>
/// Examples using OneOf library for discriminated unions
/// </summary>
public class OneOfExamples
{
    // Define error types
    public record ValidationError(string Field, string Message);

    public record NotFoundError(string Resource, object Id);

    public record UnauthorizedError(string Reason);

    public record BusinessRuleError(string Rule, string Message);

    // Example 1: Simple success or error
    public OneOf<User, NotFoundError> GetUserById(Guid userId)
    {
        if (userId == Guid.Empty)
            return new NotFoundError("User", userId);

        // Simulated user fetch
        return new User(userId, "John Doe", "john@example.com");
    }

    // Example 2: Multiple error types
    public OneOf<Order, ValidationError, NotFoundError, UnauthorizedError> CreateOrder(
        Guid userId,
        List<OrderItem> items
    )
    {
        if (userId == Guid.Empty)
            return new ValidationError("userId", "User ID is required");

        if (items == null || !items.Any())
            return new ValidationError("items", "Order must have at least one item");

        // Check if user exists
        if (!UserExists(userId))
            return new NotFoundError("User", userId);

        // Check authorization
        if (!IsAuthorized(userId))
            return new UnauthorizedError("User is not authorized to create orders");

        // Create order
        return new Order(Guid.NewGuid(), userId, items);
    }

    // Example 3: Using built-in types (Success, Error, NotFound)
    public OneOf<Success<Order>, Error<string>, NotFound> ProcessOrder(Guid orderId)
    {
        if (orderId == Guid.Empty)
            return new Error<string>("Invalid order ID");

        // Check if order exists
        if (!OrderExists(orderId))
            return new NotFound();

        // Process order
        var order = new Order(orderId, Guid.NewGuid(), new List<OrderItem>());
        return new Success<Order>(order);
    }

    // Example 4: Railway-oriented programming with OneOf
    public async Task<
        OneOf<PaymentResult, ValidationError, BusinessRuleError>
    > ProcessPaymentWorkflow(PaymentRequest request)
    {
        // Validate request
        var validationResult = ValidatePaymentRequest(request);
        if (validationResult.IsT1) // Is ValidationError
            return validationResult.AsT1;

        // Check business rules
        var businessRuleResult = await CheckBusinessRules(request);
        if (businessRuleResult.IsT1) // Is BusinessRuleError
            return businessRuleResult.AsT1;

        // Process payment
        var paymentResult = await ProcessPayment(request);
        return paymentResult;
    }

    // Example 5: Pattern matching with OneOf
    public string HandleUserResult(OneOf<User, ValidationError, NotFoundError> result)
    {
        return result.Match(
            user => $"User found: {user.Name}",
            validation => $"Validation error: {validation.Field} - {validation.Message}",
            notFound => $"Not found: {notFound.Resource} with ID {notFound.Id}"
        );
    }

    // Example 6: Async with OneOf
    public async Task<OneOf<User, NotFoundError, Error<string>>> GetUserAsync(Guid userId)
    {
        try
        {
            await Task.Delay(100); // Simulate async operation

            if (userId == Guid.Empty)
                return new NotFoundError("User", userId);

            return new User(userId, "Jane Doe", "jane@example.com");
        }
        catch (Exception ex)
        {
            return new Error<string>($"Failed to get user: {ex.Message}");
        }
    }

    // Example 7: Combining results
    public OneOf<(User user, Order order), ValidationError, NotFoundError> GetUserWithLatestOrder(
        Guid userId
    )
    {
        var userResult = GetUserById(userId);

        return userResult.Match<OneOf<(User, Order), ValidationError, NotFoundError>>(
            user =>
            {
                var orderResult = GetLatestOrderForUser(userId);
                return orderResult.Match<OneOf<(User, Order), ValidationError, NotFoundError>>(
                    order => (user, order),
                    notFound => notFound
                );
            },
            notFound => notFound
        );
    }

    // Helper methods
    private bool UserExists(Guid userId) => userId != Guid.Empty;

    private bool IsAuthorized(Guid userId) => true;

    private bool OrderExists(Guid orderId) => orderId != Guid.Empty;

    private OneOf<Success, ValidationError> ValidatePaymentRequest(PaymentRequest request)
    {
        if (request.Amount <= 0)
            return new ValidationError("amount", "Amount must be positive");
        return new Success();
    }

    private async Task<OneOf<Success, BusinessRuleError>> CheckBusinessRules(PaymentRequest request)
    {
        await Task.Delay(10);
        if (request.Amount > 10000)
            return new BusinessRuleError("MAX_AMOUNT", "Payment exceeds maximum allowed amount");
        return new Success();
    }

    private async Task<PaymentResult> ProcessPayment(PaymentRequest request)
    {
        await Task.Delay(10);
        return new PaymentResult(Guid.NewGuid(), request.Amount, DateTime.UtcNow);
    }

    private OneOf<Order, NotFoundError> GetLatestOrderForUser(Guid userId)
    {
        // Simulated order fetch
        return new Order(Guid.NewGuid(), userId, new List<OrderItem>());
    }
}

// Domain models for examples
public record User(Guid Id, string Name, string Email);

public record Order(Guid Id, Guid UserId, List<OrderItem> Items);

public record OrderItem(Guid ProductId, int Quantity, decimal Price);

public record PaymentRequest(decimal Amount, string Currency, string CardNumber);

public record PaymentResult(Guid TransactionId, decimal Amount, DateTime ProcessedAt);

// Extension methods for OneOf (must be top-level static class)
public static class OneOfExtensions
{
    public static T GetValueOr<T, TError>(this OneOf<T, TError> result, T defaultValue)
    {
        return result.Match(success => success, error => defaultValue);
    }

    public static async Task<OneOf<TNew, TError>> MapAsync<T, TNew, TError>(
        this Task<OneOf<T, TError>> resultTask,
        Func<T, Task<TNew>> mapper
    )
    {
        var result = await resultTask;
        return await result.Match<Task<OneOf<TNew, TError>>>(
            async success => await mapper(success),
            error => Task.FromResult(OneOf<TNew, TError>.FromT1(error))
        );
    }
}
