using System.Diagnostics;
using ErrorHandling.Domain.Results;
using Microsoft.AspNetCore.Mvc;

namespace ErrorHandling.Api.Extensions;

public static class ResultExtensions
{
    public static IActionResult ToProblemDetails<T>(this Result<T> result, HttpContext context)
    {
        if (result.IsSuccess)
            return new OkObjectResult(result.Value);

        return ConvertErrorToProblemDetails(result.Error!, context);
    }

    public static IActionResult ToProblemDetails(this Result result, HttpContext context)
    {
        if (result.IsSuccess)
            return new NoContentResult();

        return ConvertErrorToProblemDetails(result.Error!, context);
    }

    private static IActionResult ConvertErrorToProblemDetails(Error error, HttpContext context)
    {
        var (statusCode, type) = GetStatusAndType(error);

        var problemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = statusCode,
            Title = GetTitle(error.Type),
            Type = type,
            Detail = error.Message,
            Instance = context.Request.Path,
        };

        // Add error metadata
        problemDetails.Extensions["errorCode"] = error.Code;
        problemDetails.Extensions["errorType"] = error.Type.ToString();
        problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;
        problemDetails.Extensions["timestamp"] = System.DateTime.UtcNow;

        if (error.Metadata != null && error.Metadata.Count > 0)
        {
            foreach (var kvp in error.Metadata)
            {
                problemDetails.Extensions[kvp.Key] = kvp.Value;
            }
        }

        // Handle composite errors
        if (error is CompositeError composite)
        {
            var errors = new System.Collections.Generic.List<object>();
            foreach (var subError in composite.Errors)
            {
                errors.Add(
                    new
                    {
                        code = subError.Code,
                        message = subError.Message,
                        type = subError.Type.ToString(),
                        metadata = subError.Metadata,
                    }
                );
            }
            problemDetails.Extensions["errors"] = errors;
        }

        return new ObjectResult(problemDetails) { StatusCode = statusCode };
    }

    private static (int statusCode, string type) GetStatusAndType(Error error) =>
        error.Type switch
        {
            ErrorType.Validation => (
                StatusCodes.Status400BadRequest,
                "https://example.com/errors/validation"
            ),
            ErrorType.NotFound => (
                StatusCodes.Status404NotFound,
                "https://example.com/errors/not-found"
            ),
            ErrorType.Conflict => (
                StatusCodes.Status409Conflict,
                "https://example.com/errors/conflict"
            ),
            ErrorType.Unauthorized => (
                StatusCodes.Status401Unauthorized,
                "https://example.com/errors/unauthorized"
            ),
            ErrorType.Forbidden => (
                StatusCodes.Status403Forbidden,
                "https://example.com/errors/forbidden"
            ),
            ErrorType.Critical => (
                StatusCodes.Status500InternalServerError,
                "https://example.com/errors/critical"
            ),
            _ => (
                StatusCodes.Status422UnprocessableEntity,
                "https://example.com/errors/business-rule"
            ),
        };

    private static string GetTitle(ErrorType type) =>
        type switch
        {
            ErrorType.Validation => "Validation Error",
            ErrorType.NotFound => "Resource Not Found",
            ErrorType.Conflict => "Conflict",
            ErrorType.Unauthorized => "Unauthorized",
            ErrorType.Forbidden => "Forbidden",
            ErrorType.Critical => "Critical Error",
            _ => "Business Rule Violation",
        };
}
