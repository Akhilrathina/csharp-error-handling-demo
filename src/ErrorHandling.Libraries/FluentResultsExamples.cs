using FluentResults;

namespace ErrorHandling.Libraries;

/// <summary>
/// Examples using FluentResults library
/// </summary>
public class FluentResultsExamples
{
    // Example 1: Basic Result usage
    public Result<Customer> CreateCustomer(string name, string email)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Fail<Customer>("Customer name is required");

        if (string.IsNullOrWhiteSpace(email))
            return Result.Fail<Customer>("Customer email is required");

        if (!IsValidEmail(email))
            return Result
                .Fail<Customer>("Invalid email format")
                .WithError(new Error("Email validation failed").WithMetadata("email", email));

        var customer = new Customer(Guid.NewGuid(), name, email);

        return Result.Ok(customer).WithSuccess("Customer created successfully");
    }

    // Example 2: Multiple errors
    public Result ValidateProduct(Product product)
    {
        var result = new Result();

        if (string.IsNullOrWhiteSpace(product.Name))
            result.WithError("Product name is required");

        if (product.Price <= 0)
            result.WithError(
                new Error("Product price must be positive").WithMetadata("price", product.Price)
            );

        if (product.Stock < 0)
            result.WithError("Product stock cannot be negative");

        return result;
    }

    // Example 3: Chaining operations
    public Result<Invoice> CreateInvoice(Guid customerId, List<InvoiceItem> items)
    {
        var result = GetCustomer(customerId)
            .Bind(customer => ValidateCustomerCredit(customer, items))
            .Bind(customer => CreateInvoiceForCustomer(customer, items));

        if (result.IsSuccess)
            LogInvoiceCreation(result.Value);

        return result;
    }

    // Example 4: Custom error types
    public class ValidationError : Error
    {
        public string Field { get; }

        public ValidationError(string field, string message)
            : base(message)
        {
            Field = field;
            WithMetadata("field", field);
        }
    }

    public class BusinessRuleError : Error
    {
        public string RuleName { get; }

        public BusinessRuleError(string ruleName, string message)
            : base(message)
        {
            RuleName = ruleName;
            WithMetadata("rule", ruleName);
        }
    }

    public Result<decimal> CalculateDiscount(Customer customer, decimal orderAmount)
    {
        if (orderAmount <= 0)
            return Result.Fail<decimal>(
                new ValidationError("orderAmount", "Order amount must be positive")
            );

        if (!customer.IsEligibleForDiscount)
            return Result.Fail<decimal>(
                new BusinessRuleError(
                    "DISCOUNT_ELIGIBILITY",
                    "Customer is not eligible for discount"
                )
            );

        var discount = orderAmount * 0.1m;

        return Result.Ok(discount).WithSuccess($"Discount of {discount:C} applied");
    }

    // Example 5: Async operations with error handling
    public async Task<Result<PaymentConfirmation>> ProcessPaymentAsync(PaymentDetails payment)
    {
        // Validate payment
        var validationResult = ValidatePayment(payment);
        if (validationResult.IsFailed)
            return validationResult.ToResult<PaymentConfirmation>();

        try
        {
            // Process payment
            var confirmation = await CallPaymentGateway(payment);

            return Result
                .Ok(confirmation)
                .WithSuccess("Payment processed successfully")
                .WithSuccess($"Transaction ID: {confirmation.TransactionId}");
        }
        catch (PaymentGatewayException ex)
        {
            return Result
                .Fail<PaymentConfirmation>("Payment gateway error")
                .WithError(
                    new Error(ex.Message).CausedBy(ex).WithMetadata("gatewayCode", ex.ErrorCode)
                );
        }
        catch (Exception ex)
        {
            return Result
                .Fail<PaymentConfirmation>("Unexpected error during payment processing")
                .WithError(new Error("Internal error").CausedBy(ex));
        }
    }

    // Example 6: Combining multiple results
    public Result<ShippingLabel> PrepareShipment(Guid orderId)
    {
        var results = new List<Result>
        {
            ValidateOrder(orderId),
            CheckInventory(orderId),
            VerifyShippingAddress(orderId),
        };

        var combinedResult = Result.Merge(results.ToArray());

        if (combinedResult.IsFailed)
            return combinedResult.ToResult<ShippingLabel>();

        return GenerateShippingLabel(orderId);
    }

    // Example 7: Result with reasons (success and error)
    public Result<Account> OpenAccount(AccountApplication application)
    {
        var result = new Result<Account>();

        // Validate application
        if (!IsValidApplication(application))
        {
            result
                .WithError("Invalid application")
                .WithError(
                    new Error("Application validation failed").WithMetadata(
                        "applicationId",
                        application.Id
                    )
                );
            return result;
        }

        // Check credit score
        var creditCheck = CheckCreditScore(application.ApplicantId);
        if (creditCheck < 600)
        {
            result.WithError(
                new BusinessRuleError(
                    "CREDIT_CHECK",
                    $"Credit score {creditCheck} is below minimum requirement"
                )
            );
            return result;
        }

        // Create account
        var account = new Account(Guid.NewGuid(), application.ApplicantId);

        result
            .WithValue(account)
            .WithSuccess("Account opened successfully")
            .WithSuccess(new Success($"Account number: {account.Number}"))
            .WithSuccess(new Success("Welcome package will be sent"));

        return result;
    }

    // Example 9: Working with metadata and error context
    public Result<Reservation> MakeReservation(ReservationRequest request)
    {
        var result = new Result<Reservation>();

        // Check availability
        if (!IsAvailable(request.Date, request.Slots))
        {
            var error = new Error("Requested slots are not available")
                .WithMetadata("requestedDate", request.Date)
                .WithMetadata("requestedSlots", request.Slots)
                .WithMetadata("availableSlots", GetAvailableSlots(request.Date));

            result.WithError(error);
            return result;
        }

        // Create reservation
        var reservation = new Reservation(
            Guid.NewGuid(),
            request.CustomerId,
            request.Date,
            request.Slots
        );

        // Add contextual success information
        result
            .WithValue(reservation)
            .WithSuccess($"Reservation confirmed for {request.Date:yyyy-MM-dd}")
            .WithSuccess(
                new Success("Confirmation email sent").WithMetadata("emailSentAt", DateTime.UtcNow)
            );

        return result;
    }

    // Helper methods
    private bool IsValidEmail(string email) => email.Contains("@");

    private Result<Customer> GetCustomer(Guid customerId)
    {
        if (customerId == Guid.Empty)
            return Result.Fail<Customer>("Invalid customer ID");
        return Result.Ok(new Customer(customerId, "John Doe", "john@example.com"));
    }

    private Result<Customer> ValidateCustomerCredit(Customer customer, List<InvoiceItem> items)
    {
        var total = items.Sum(i => i.Price * i.Quantity);
        if (total > customer.CreditLimit)
            return Result.Fail<Customer>("Insufficient credit limit");
        return Result.Ok(customer);
    }

    private Result<Invoice> CreateInvoiceForCustomer(Customer customer, List<InvoiceItem> items)
    {
        var invoice = new Invoice(Guid.NewGuid(), customer.Id, items);
        return Result.Ok(invoice);
    }

    private void LogInvoiceCreation(Invoice invoice)
    {
        // Log invoice creation
        Console.WriteLine($"Invoice {invoice.Id} created");
    }

    private Result ValidatePayment(PaymentDetails payment)
    {
        if (payment.Amount <= 0)
            return Result.Fail("Invalid payment amount");
        return Result.Ok();
    }

    private async Task<PaymentConfirmation> CallPaymentGateway(PaymentDetails payment)
    {
        await Task.Delay(100);
        return new PaymentConfirmation(Guid.NewGuid().ToString(), DateTime.UtcNow);
    }

    private Result ValidateOrder(Guid orderId) => Result.Ok();

    private Result CheckInventory(Guid orderId) => Result.Ok();

    private Result VerifyShippingAddress(Guid orderId) => Result.Ok();

    private Result<ShippingLabel> GenerateShippingLabel(Guid orderId) =>
        Result.Ok(new ShippingLabel(orderId, "123 Main St"));

    private bool IsValidApplication(AccountApplication app) => app != null;

    private int CheckCreditScore(Guid applicantId) => 650;

    private bool IsAvailable(DateTime date, int slots) => true;

    private int GetAvailableSlots(DateTime date) => 5;
}

// Domain models for examples
public record Customer(Guid Id, string Name, string Email)
{
    public decimal CreditLimit { get; init; } = 10000;
    public bool IsEligibleForDiscount { get; init; } = true;
}

public record Product(Guid Id, string Name, decimal Price, int Stock);

public record Invoice(Guid Id, Guid CustomerId, List<InvoiceItem> Items);

public record InvoiceItem(string Description, decimal Price, int Quantity);

public record PaymentDetails(decimal Amount, string CardNumber, string Cvv);

public record PaymentConfirmation(string TransactionId, DateTime ProcessedAt);

public record ShippingLabel(Guid OrderId, string Address);

public record AccountApplication(Guid Id, Guid ApplicantId);

public record Account(Guid Id, Guid CustomerId)
{
    public string Number { get; } = Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper();
}

public record ReservationRequest(Guid CustomerId, DateTime Date, int Slots);

public record Reservation(Guid Id, Guid CustomerId, DateTime Date, int Slots);

public class PaymentGatewayException : Exception
{
    public string ErrorCode { get; }

    public PaymentGatewayException(string message, string errorCode)
        : base(message)
    {
        ErrorCode = errorCode;
    }
}

// Extension methods for FluentResults (must be top-level static class)
public static class FluentResultsExtensions
{
    public static Result<T> Ensure<T>(
        this Result<T> result,
        Func<T, bool> predicate,
        string errorMessage
    )
    {
        if (result.IsFailed)
            return result;

        if (!predicate(result.Value))
            return Result.Fail<T>(errorMessage);

        return result;
    }

    public static async Task<Result<TNew>> BindAsync<T, TNew>(
        this Task<Result<T>> resultTask,
        Func<T, Task<Result<TNew>>> binder
    )
    {
        var result = await resultTask;
        if (result.IsFailed)
            return result.ToResult<TNew>();

        return await binder(result.Value);
    }

    public static Result<T> LogIfFailed<T>(
        this Result<T> result,
        Action<IEnumerable<IError>> logAction
    )
    {
        if (result.IsFailed)
            logAction(result.Errors);
        return result;
    }
}
