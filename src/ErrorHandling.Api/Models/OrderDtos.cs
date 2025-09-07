namespace ErrorHandling.Api.Models;

public record CreateOrderRequest(Guid CustomerId, string ShippingAddress);

public record AddItemRequest(Guid ProductId, int Quantity);

public record ProcessPaymentRequest(decimal Amount, string Currency = "USD");

public record CancelOrderRequest(string Reason);

public record OrderResponse(
    Guid Id,
    Guid CustomerId,
    string ShippingAddress,
    decimal TotalAmount,
    string Currency,
    string Status,
    DateTime CreatedAt,
    DateTime? ShippedAt,
    List<OrderItemResponse> Items
);

public record OrderItemResponse(
    Guid ProductId,
    string ProductName,
    decimal Price,
    string Currency,
    int Quantity,
    decimal TotalPrice
);

public record OrderWorkflowRequest(
    Guid CustomerId,
    string ShippingAddress,
    List<OrderItemRequest> Items,
    decimal PaymentAmount,
    string PaymentCurrency
);

public record OrderItemRequest(Guid ProductId, int Quantity);

