using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ErrorHandling.Api.ProblemDetails;

public class CustomProblemDetailsFactory : ProblemDetailsFactory
{
    private readonly IHostEnvironment _environment;

    public CustomProblemDetailsFactory(IHostEnvironment environment)
    {
        _environment = environment;
    }

    public override Microsoft.AspNetCore.Mvc.ProblemDetails CreateProblemDetails(
        HttpContext httpContext,
        int? statusCode = null,
        string? title = null,
        string? type = null,
        string? detail = null,
        string? instance = null
    )
    {
        var problemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = statusCode ?? StatusCodes.Status500InternalServerError,
            Title = title ?? GetDefaultTitle(statusCode),
            Type = type ?? GetDefaultType(statusCode),
            Detail = detail,
            Instance = instance ?? httpContext.Request.Path,
        };

        ApplyCommonExtensions(problemDetails, httpContext);

        return problemDetails;
    }

    public override ValidationProblemDetails CreateValidationProblemDetails(
        HttpContext httpContext,
        ModelStateDictionary modelStateDictionary,
        int? statusCode = null,
        string? title = null,
        string? type = null,
        string? detail = null,
        string? instance = null
    )
    {
        var problemDetails = new ValidationProblemDetails(modelStateDictionary)
        {
            Status = statusCode ?? StatusCodes.Status400BadRequest,
            Title = title ?? "Validation Error",
            Type = type ?? "https://example.com/errors/validation",
            Detail = detail ?? "One or more validation errors occurred",
            Instance = instance ?? httpContext.Request.Path,
        };

        ApplyCommonExtensions(problemDetails, httpContext);

        return problemDetails;
    }

    private void ApplyCommonExtensions(
        Microsoft.AspNetCore.Mvc.ProblemDetails problemDetails,
        HttpContext httpContext
    )
    {
        problemDetails.Extensions["timestamp"] = DateTimeOffset.UtcNow;
        problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        var correlationId =
            httpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? Guid.NewGuid()
                .ToString();
        problemDetails.Extensions["correlationId"] = correlationId;

        problemDetails.Extensions["apiVersion"] = "1.0.0";
        problemDetails.Extensions["environment"] = _environment.EnvironmentName;

        if (!_environment.IsProduction())
        {
            problemDetails.Extensions["machineName"] = Environment.MachineName;

            var requestId = httpContext.Request.Headers["X-Request-Id"].FirstOrDefault();
            if (!string.IsNullOrEmpty(requestId))
                problemDetails.Extensions["requestId"] = requestId;

            problemDetails.Extensions["path"] = httpContext.Request.Path.Value;
            problemDetails.Extensions["method"] = httpContext.Request.Method;
        }
    }

    private string GetDefaultTitle(int? statusCode) =>
        statusCode switch
        {
            400 => "Bad Request",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "Not Found",
            409 => "Conflict",
            422 => "Unprocessable Entity",
            500 => "Internal Server Error",
            _ => "Error",
        };

    private string GetDefaultType(int? statusCode) =>
        statusCode switch
        {
            400 => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1",
            401 => "https://datatracker.ietf.org/doc/html/rfc7235#section-3.1",
            403 => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.3",
            404 => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4",
            409 => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.8",
            422 => "https://datatracker.ietf.org/doc/html/rfc4918#section-11.2",
            500 => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1",
            _ => "https://datatracker.ietf.org/doc/html/rfc7231#section-6",
        };
}
