using ErrorHandling.Api.Extensions;
using ErrorHandling.Api.Models;
using ErrorHandling.Domain.Entities;
using ErrorHandling.Domain.Results;
using ErrorHandling.Domain.Services;
using ErrorHandling.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;

namespace ErrorHandling.Api.Controllers;

/// <summary>
/// Controller demonstrating Result pattern error handling
/// Results are converted to appropriate HTTP responses
/// </summary>
[ApiController]
[Route("api/v1/result/orders")]
public class OrdersResultController : ControllerBase
{
    private readonly ResultOrderService _orderService;
    private readonly ILogger<OrdersResultController> _logger;

    public OrdersResultController(
        ResultOrderService orderService,
        ILogger<OrdersResultController> logger
    )
    {
        _orderService = orderService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new order (Result pattern approach)
    /// </summary>
    /// <remarks>
    /// This endpoint uses the Result pattern.
    /// Errors are returned as Result failures and converted to Problem Details.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(Order), StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(Microsoft.AspNetCore.Mvc.ProblemDetails),
        StatusCodes.Status400BadRequest
    )]
    [ProducesResponseType(
        typeof(Microsoft.AspNetCore.Mvc.ProblemDetails),
        StatusCodes.Status404NotFound
    )]
    [ProducesResponseType(
        typeof(Microsoft.AspNetCore.Mvc.ProblemDetails),
        StatusCodes.Status422UnprocessableEntity
    )]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        _logger.LogInformation("Creating order for customer {CustomerId}", request.CustomerId);

        var result = await _orderService.CreateOrderAsync(
            request.CustomerId,
            request.ShippingAddress
        );

        return result.Match(
            success =>
            {
                _logger.LogInformation("Order {OrderId} created successfully", success.Id);
                return CreatedAtAction(nameof(GetOrder), new { orderId = success.Id }, success);
            },
            failure =>
            {
                _logger.LogWarning("Failed to create order: {Error}", failure.Message);
                return result.ToProblemDetails(HttpContext);
            }
        );
    }

    [HttpGet("{orderId}")]
    [ProducesResponseType(typeof(Order), StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(Microsoft.AspNetCore.Mvc.ProblemDetails),
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> GetOrder(Guid orderId)
    {
        // Validation using Result
        if (orderId == Guid.Empty)
        {
            var error = Error.Validation("orderId", "Invalid order ID");
            return Result<Order>.Failure(error).ToProblemDetails(HttpContext);
        }

        // Simulated order retrieval
        await Task.Delay(10);

        // For demo, returning a result
        var order = new Order(Guid.NewGuid(), "123 Main St");
        return Result<Order>.Success(order).ToProblemDetails(HttpContext);
    }

    [HttpPost("{orderId}/items")]
    [ProducesResponseType(typeof(Order), StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(Microsoft.AspNetCore.Mvc.ProblemDetails),
        StatusCodes.Status400BadRequest
    )]
    [ProducesResponseType(
        typeof(Microsoft.AspNetCore.Mvc.ProblemDetails),
        StatusCodes.Status404NotFound
    )]
    [ProducesResponseType(
        typeof(Microsoft.AspNetCore.Mvc.ProblemDetails),
        StatusCodes.Status422UnprocessableEntity
    )]
    public async Task<IActionResult> AddItem(Guid orderId, [FromBody] AddItemRequest request)
    {
        _logger.LogInformation(
            "Adding item {ProductId} to order {OrderId}",
            request.ProductId,
            orderId
        );

        var result = await _orderService.AddItemToOrderAsync(
            orderId,
            request.ProductId,
            request.Quantity
        );

        return result.Match(
            success =>
            {
                _logger.LogInformation("Item added to order {OrderId}", orderId);
                return Ok(success);
            },
            failure =>
            {
                _logger.LogWarning("Failed to add item: {Error}", failure.Message);
                return result.ToProblemDetails(HttpContext);
            }
        );
    }

    [HttpPost("{orderId}/submit")]
    [ProducesResponseType(typeof(Order), StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(Microsoft.AspNetCore.Mvc.ProblemDetails),
        StatusCodes.Status404NotFound
    )]
    [ProducesResponseType(
        typeof(Microsoft.AspNetCore.Mvc.ProblemDetails),
        StatusCodes.Status422UnprocessableEntity
    )]
    public async Task<IActionResult> SubmitOrder(Guid orderId)
    {
        _logger.LogInformation("Submitting order {OrderId}", orderId);

        var result = await _orderService.SubmitOrderAsync(orderId);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Order {OrderId} submitted successfully", orderId);
            return Ok(result.Value);
        }

        _logger.LogWarning(
            "Failed to submit order {OrderId}: {Error}",
            orderId,
            result.Error!.Message
        );
        return result.ToProblemDetails(HttpContext);
    }

    [HttpPost("{orderId}/payment")]
    [ProducesResponseType(typeof(Order), StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(Microsoft.AspNetCore.Mvc.ProblemDetails),
        StatusCodes.Status400BadRequest
    )]
    [ProducesResponseType(
        typeof(Microsoft.AspNetCore.Mvc.ProblemDetails),
        StatusCodes.Status404NotFound
    )]
    [ProducesResponseType(
        typeof(Microsoft.AspNetCore.Mvc.ProblemDetails),
        StatusCodes.Status422UnprocessableEntity
    )]
    public async Task<IActionResult> ProcessPayment(Guid orderId, [FromBody] ProcessPaymentRequest request)
    {
        _logger.LogInformation("Processing payment for order {OrderId}", orderId);

        // Create Money using Result pattern
        var moneyResult = Money.TryCreate(request.Amount, request.Currency);
        if (moneyResult.IsFailure)
            return Result<Order>.Failure(moneyResult.Error!).ToProblemDetails(HttpContext);

        var result = await _orderService.ProcessPaymentAsync(orderId, moneyResult.Value);

        return result.ToProblemDetails(HttpContext);
    }

    [HttpPost("{orderId}/ship")]
    [ProducesResponseType(typeof(Order), StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(Microsoft.AspNetCore.Mvc.ProblemDetails),
        StatusCodes.Status404NotFound
    )]
    [ProducesResponseType(
        typeof(Microsoft.AspNetCore.Mvc.ProblemDetails),
        StatusCodes.Status422UnprocessableEntity
    )]
    public async Task<IActionResult> ShipOrder(Guid orderId)
    {
        _logger.LogInformation("Shipping order {OrderId}", orderId);

        var result = await _orderService.ShipOrderAsync(orderId);

        result
            .OnSuccess(order =>
                _logger.LogInformation("Order {OrderId} shipped successfully", orderId)
            )
            .OnFailure(error =>
                _logger.LogWarning(
                    "Failed to ship order {OrderId}: {Error}",
                    orderId,
                    error.Message
                )
            );

        return result.ToProblemDetails(HttpContext);
    }

    [HttpPost("{orderId}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        typeof(Microsoft.AspNetCore.Mvc.ProblemDetails),
        StatusCodes.Status400BadRequest
    )]
    [ProducesResponseType(
        typeof(Microsoft.AspNetCore.Mvc.ProblemDetails),
        StatusCodes.Status404NotFound
    )]
    [ProducesResponseType(
        typeof(Microsoft.AspNetCore.Mvc.ProblemDetails),
        StatusCodes.Status422UnprocessableEntity
    )]
    public async Task<IActionResult> CancelOrder(
        Guid orderId,
        [FromBody] CancelOrderRequest request
    )
    {
        _logger.LogInformation(
            "Cancelling order {OrderId} with reason: {Reason}",
            orderId,
            request.Reason
        );

        var result = await _orderService.CancelOrderAsync(orderId, request.Reason);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Order {OrderId} cancelled successfully", orderId);
            return NoContent();
        }

        _logger.LogWarning(
            "Failed to cancel order {OrderId}: {Error}",
            orderId,
            result.Error!.Message
        );
        return result.ToProblemDetails(HttpContext);
    }

    /// <summary>
    /// Example of railway-oriented programming with Result
    /// </summary>
    [HttpPost("workflow")]
    [ProducesResponseType(typeof(Order), StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(Microsoft.AspNetCore.Mvc.ProblemDetails),
        StatusCodes.Status400BadRequest
    )]
    [ProducesResponseType(
        typeof(Microsoft.AspNetCore.Mvc.ProblemDetails),
        StatusCodes.Status404NotFound
    )]
    [ProducesResponseType(
        typeof(Microsoft.AspNetCore.Mvc.ProblemDetails),
        StatusCodes.Status422UnprocessableEntity
    )]
    public async Task<IActionResult> ProcessOrderWorkflow([FromBody] OrderWorkflowRequest request)
    {
        _logger.LogInformation(
            "Processing order workflow for customer {CustomerId}",
            request.CustomerId
        );

        var moneyResult = Money.TryCreate(request.PaymentAmount, request.PaymentCurrency);
        if (moneyResult.IsFailure)
            return Result<Order>.Failure(moneyResult.Error!).ToProblemDetails(HttpContext);

        var items = request.Items.Select(i => (i.ProductId, i.Quantity)).ToArray();

        var result = await _orderService.ProcessOrderWorkflowAsync(
            request.CustomerId,
            request.ShippingAddress,
            items,
            moneyResult.Value
        );

        return result.ToProblemDetails(HttpContext);
    }
}
