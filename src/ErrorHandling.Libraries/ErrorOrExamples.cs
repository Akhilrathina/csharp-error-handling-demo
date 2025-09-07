using ErrorOr;

namespace ErrorHandling.Libraries;

/// <summary>
/// Examples using ErrorOr library - lightweight and modern approach
/// </summary>
public class ErrorOrExamples
{
    // Example 1: Basic ErrorOr usage
    public ErrorOr<UserProfile> GetUserProfile(Guid userId)
    {
        if (userId == Guid.Empty)
            return Error.Validation("User.InvalidId", "User ID cannot be empty");

        // Simulate user not found
        if (userId.ToString().StartsWith("0"))
            return Error.NotFound("User.NotFound", $"User with ID {userId} was not found");

        return new UserProfile(userId, "John Doe", "john@example.com", true);
    }

    // Example 2: Multiple errors
    public ErrorOr<BankAccount> CreateBankAccount(
        string accountNumber,
        string ownerName,
        decimal initialDeposit
    )
    {
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(accountNumber))
            errors.Add(Error.Validation("Account.InvalidNumber", "Account number is required"));

        if (accountNumber?.Length != 10)
            errors.Add(
                Error.Validation("Account.InvalidNumberLength", "Account number must be 10 digits")
            );

        if (string.IsNullOrWhiteSpace(ownerName))
            errors.Add(Error.Validation("Account.InvalidOwner", "Owner name is required"));

        if (initialDeposit < 100)
            errors.Add(
                Error.Validation(
                    "Account.InsufficientDeposit",
                    "Initial deposit must be at least $100"
                )
            );

        if (errors.Any())
            return errors;

        return new BankAccount(accountNumber!, ownerName!, initialDeposit);
    }

    // Example 3: Built-in error types
    public ErrorOr<TransferResult> TransferMoney(
        string fromAccount,
        string toAccount,
        decimal amount
    )
    {
        // Validation error
        if (amount <= 0)
            return Error.Validation("Transfer.InvalidAmount", "Transfer amount must be positive");

        // Conflict error
        if (fromAccount == toAccount)
            return Error.Conflict("Transfer.SameAccount", "Cannot transfer to the same account");

        // Not found error
        if (!AccountExists(fromAccount))
            return Error.NotFound(
                "Transfer.FromAccountNotFound",
                $"Source account {fromAccount} not found"
            );

        if (!AccountExists(toAccount))
            return Error.NotFound(
                "Transfer.ToAccountNotFound",
                $"Destination account {toAccount} not found"
            );

        // Failure error (business rule)
        if (!HasSufficientBalance(fromAccount, amount))
            return Error.Failure("Transfer.InsufficientFunds", "Insufficient funds for transfer");

        // Forbidden error
        if (IsAccountFrozen(fromAccount))
            return Error.Forbidden("Transfer.AccountFrozen", "Cannot transfer from frozen account");

        // Unauthorized error
        if (!IsAuthorizedForTransfer(fromAccount))
            return Error.Unauthorized(
                "Transfer.Unauthorized",
                "Not authorized to perform transfer"
            );

        // Success
        return new TransferResult(
            Guid.NewGuid().ToString(),
            fromAccount,
            toAccount,
            amount,
            DateTime.UtcNow
        );
    }

    // Example 4: Custom error types
    public static class DomainErrors
    {
        public static class Order
        {
            public static Error EmptyCart =>
                Error.Validation("Order.EmptyCart", "Cannot create order with empty cart");

            public static Error InvalidQuantity(int quantity) =>
                Error.Validation("Order.InvalidQuantity", $"Quantity {quantity} is invalid");

            public static Error InsufficientStock(string product, int available) =>
                Error.Conflict(
                    "Order.InsufficientStock",
                    $"Product {product} has only {available} items in stock"
                );

            public static Error CustomerSuspended =>
                Error.Forbidden("Order.CustomerSuspended", "Customer account is suspended");
        }

        public static class Payment
        {
            public static Error InvalidCard =>
                Error.Validation("Payment.InvalidCard", "Credit card information is invalid");

            public static Error ExpiredCard =>
                Error.Failure("Payment.ExpiredCard", "Credit card has expired");

            public static Error PaymentDeclined(string reason) =>
                Error.Failure("Payment.Declined", $"Payment was declined: {reason}");

            public static Error ExceedsLimit(decimal limit) =>
                Error.Conflict("Payment.ExceedsLimit", $"Payment exceeds daily limit of ${limit}");
        }
    }

    // Example 5: Using custom domain errors
    public ErrorOr<PurchaseOrder> CreatePurchaseOrder(Guid customerId, List<CartItem> cartItems)
    {
        if (!cartItems.Any())
            return DomainErrors.Order.EmptyCart;

        foreach (var item in cartItems)
        {
            if (item.Quantity <= 0)
                return DomainErrors.Order.InvalidQuantity(item.Quantity);

            var stockLevel = GetStockLevel(item.ProductId);
            if (stockLevel < item.Quantity)
                return DomainErrors.Order.InsufficientStock(item.ProductName, stockLevel);
        }

        if (IsCustomerSuspended(customerId))
            return DomainErrors.Order.CustomerSuspended;

        return new PurchaseOrder(Guid.NewGuid(), customerId, cartItems);
    }

    // Example 6: Railway-oriented programming with ErrorOr
    public async Task<ErrorOr<OrderConfirmation>> ProcessOrderWorkflow(OrderRequest request)
    {
        // Validate order
        var validationResult = ValidateOrderRequest(request);
        if (validationResult.IsError)
            return validationResult.Errors;

        // Check inventory
        var inventoryResult = await CheckInventoryAsync(request.Items);
        if (inventoryResult.IsError)
            return inventoryResult.Errors;

        // Process payment
        var paymentResult = await ProcessPaymentAsync(request.Payment);
        if (paymentResult.IsError)
            return paymentResult.Errors;

        // Create order
        var orderResult = await CreateOrderAsync(request, paymentResult.Value);
        if (orderResult.IsError)
            return orderResult.Errors;

        return new OrderConfirmation(
            orderResult.Value.Id,
            paymentResult.Value.TransactionId,
            DateTime.UtcNow
        );
    }

    // Example 7: Pattern matching with ErrorOr
    public string HandleTransferResult(ErrorOr<TransferResult> result)
    {
        return result.Match(
            success => $"Transfer successful: {success.TransactionId}",
            errors => $"Transfer failed: {string.Join(", ", errors.Select(e => e.Description))}"
        );
    }

    // Example 9: Combining multiple ErrorOr results
    public ErrorOr<ShippingInfo> CalculateShipping(
        string zipCode,
        decimal orderWeight,
        ShippingMethod method
    )
    {
        // Validate zip code
        var zipValidation = ValidateZipCode(zipCode);
        if (zipValidation.IsError)
            return zipValidation.Errors;

        // Validate weight
        var weightValidation = ValidateWeight(orderWeight);
        if (weightValidation.IsError)
            return weightValidation.Errors;

        // Calculate cost
        var costResult = CalculateShippingCost(zipCode, orderWeight, method);
        if (costResult.IsError)
            return costResult.Errors;

        // Estimate delivery
        var deliveryResult = EstimateDelivery(zipCode, method);
        if (deliveryResult.IsError)
            return deliveryResult.Errors;

        return new ShippingInfo(method, costResult.Value, deliveryResult.Value);
    }

    // Example 10: First error wins vs all errors
    public class ValidationExample
    {
        // Returns first error encountered
        public ErrorOr<Subscription> ValidateSubscriptionFirstError(SubscriptionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                return Error.Validation("Subscription.InvalidEmail", "Email is required");

            if (request.PlanId == Guid.Empty)
                return Error.Validation("Subscription.InvalidPlan", "Plan ID is required");

            if (request.StartDate < DateTime.UtcNow.Date)
                return Error.Validation(
                    "Subscription.InvalidStartDate",
                    "Start date cannot be in the past"
                );

            return new Subscription(
                Guid.NewGuid(),
                request.Email,
                request.PlanId,
                request.StartDate
            );
        }

        // Collects all errors
        public ErrorOr<Subscription> ValidateSubscriptionAllErrors(SubscriptionRequest request)
        {
            var errors = new List<Error>();

            if (string.IsNullOrWhiteSpace(request.Email))
                errors.Add(Error.Validation("Subscription.InvalidEmail", "Email is required"));

            if (request.PlanId == Guid.Empty)
                errors.Add(Error.Validation("Subscription.InvalidPlan", "Plan ID is required"));

            if (request.StartDate < DateTime.UtcNow.Date)
                errors.Add(
                    Error.Validation(
                        "Subscription.InvalidStartDate",
                        "Start date cannot be in the past"
                    )
                );

            if (errors.Any())
                return errors;

            return new Subscription(
                Guid.NewGuid(),
                request.Email,
                request.PlanId,
                request.StartDate
            );
        }
    }

    // Helper methods
    private bool AccountExists(string accountNumber) => !string.IsNullOrEmpty(accountNumber);

    private bool HasSufficientBalance(string account, decimal amount) => true;

    private bool IsAccountFrozen(string account) => false;

    private bool IsAuthorizedForTransfer(string account) => true;

    private int GetStockLevel(Guid productId) => 10;

    private bool IsCustomerSuspended(Guid customerId) => false;

    private ErrorOr<bool> ValidateOrderRequest(OrderRequest request) =>
        request != null ? true : Error.Validation("Order.Invalid", "Invalid order request");

    private async Task<ErrorOr<bool>> CheckInventoryAsync(List<OrderRequestItem> items)
    {
        await Task.Delay(10);
        return true;
    }

    private async Task<ErrorOr<PaymentReceipt>> ProcessPaymentAsync(PaymentInfo payment)
    {
        await Task.Delay(10);
        return new PaymentReceipt(Guid.NewGuid().ToString(), payment.Amount);
    }

    private async Task<ErrorOr<CreatedOrder>> CreateOrderAsync(
        OrderRequest request,
        PaymentReceipt receipt
    )
    {
        await Task.Delay(10);
        return new CreatedOrder(Guid.NewGuid(), request.CustomerId);
    }

    private ErrorOr<bool> ValidateZipCode(string zipCode) =>
        !string.IsNullOrEmpty(zipCode) && zipCode.Length == 5
            ? true
            : Error.Validation("Shipping.InvalidZipCode", "Invalid zip code");

    private ErrorOr<bool> ValidateWeight(decimal weight) =>
        weight > 0 && weight < 100
            ? true
            : Error.Validation("Shipping.InvalidWeight", "Weight must be between 0 and 100 lbs");

    private ErrorOr<decimal> CalculateShippingCost(
        string zipCode,
        decimal weight,
        ShippingMethod method
    ) =>
        method switch
        {
            ShippingMethod.Standard => 5.99m,
            ShippingMethod.Express => 15.99m,
            ShippingMethod.Overnight => 29.99m,
            _ => Error.Failure("Shipping.UnknownMethod", "Unknown shipping method"),
        };

    private ErrorOr<DateTime> EstimateDelivery(string zipCode, ShippingMethod method) =>
        method switch
        {
            ShippingMethod.Standard => DateTime.UtcNow.AddDays(5),
            ShippingMethod.Express => DateTime.UtcNow.AddDays(2),
            ShippingMethod.Overnight => DateTime.UtcNow.AddDays(1),
            _ => Error.Failure("Shipping.UnknownMethod", "Unknown shipping method"),
        };
}

// Domain models for ErrorOr examples
public record UserProfile(Guid Id, string Name, string Email, bool IsActive);

public record BankAccount(string AccountNumber, string OwnerName, decimal Balance);

public record TransferResult(
    string TransactionId,
    string FromAccount,
    string ToAccount,
    decimal Amount,
    DateTime Timestamp
);

public record CartItem(Guid ProductId, string ProductName, int Quantity, decimal Price);

public record PurchaseOrder(Guid Id, Guid CustomerId, List<CartItem> Items);

public record OrderRequest(Guid CustomerId, List<OrderRequestItem> Items, PaymentInfo Payment);

public record OrderRequestItem(Guid ProductId, int Quantity);

public record PaymentInfo(string CardNumber, decimal Amount);

public record PaymentReceipt(string TransactionId, decimal Amount);

public record CreatedOrder(Guid Id, Guid CustomerId);

public record OrderConfirmation(Guid OrderId, string PaymentTransactionId, DateTime ConfirmedAt);

public enum ShippingMethod
{
    Standard,
    Express,
    Overnight,
}

public record ShippingInfo(ShippingMethod Method, decimal Cost, DateTime EstimatedDelivery);

public record SubscriptionRequest(string Email, Guid PlanId, DateTime StartDate);

public record Subscription(Guid Id, string Email, Guid PlanId, DateTime StartDate);

// Extension methods for ErrorOr (must be top-level static class)
public static class ErrorOrExtensions
{
    public static ErrorOr<T> Ensure<T>(
        this ErrorOr<T> errorOr,
        Func<T, bool> predicate,
        Error error
    )
    {
        if (errorOr.IsError)
            return errorOr;

        return predicate(errorOr.Value) ? errorOr : error;
    }

    public static async Task<ErrorOr<TNew>> ThenAsync<T, TNew>(
        this Task<ErrorOr<T>> task,
        Func<T, Task<ErrorOr<TNew>>> next
    )
    {
        var result = await task;
        if (result.IsError)
            return result.Errors;

        return await next(result.Value);
    }

    public static ErrorOr<T> Tap<T>(this ErrorOr<T> errorOr, Action<T> action)
    {
        if (!errorOr.IsError)
            action(errorOr.Value);
        return errorOr;
    }
}
