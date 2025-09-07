namespace ErrorHandling.Domain.Exceptions;

public abstract class DomainException : Exception
{
    // RFC 7807 Problem Details properties
    public string Type { get; }
    public string Title { get; }
    public int Status { get; }
    public string Detail { get; }
    public string Instance { get; }

    // Additional context properties
    public string Code { get; }
    public DateTime Timestamp { get; }
    public string CorrelationId { get; }
    public string? UserId { get; set; }
    public string? TenantId { get; set; }

    // Validation and business rule context
    public IDictionary<string, object> Extensions { get; }
    public IList<ValidationError> ValidationErrors { get; }

    protected DomainException(
        string message,
        string? code = null,
        int status = 400,
        string? correlationId = null,
        IDictionary<string, object>? extensions = null
    )
        : base(message)
    {
        Code = code ?? GetType().Name.ToUpperInvariant();
        Detail = message;
        Title = GetTitle();
        Type = $"https://yourdomain.com/errors/{Code.ToLowerInvariant()}";
        Status = status;
        Timestamp = DateTime.UtcNow;
        CorrelationId = correlationId ?? Guid.NewGuid().ToString();
        Instance = $"/errors/{CorrelationId}";
        Extensions = extensions ?? new Dictionary<string, object>();
        ValidationErrors = new List<ValidationError>();
    }

    protected virtual string GetTitle() => "Domain Error";

    public DomainException WithUserId(string? userId)
    {
        UserId = userId;
        return this;
    }

    public DomainException WithTenantId(string? tenantId)
    {
        TenantId = tenantId;
        return this;
    }

    public DomainException WithExtension(string key, object value)
    {
        Extensions[key] = value;
        return this;
    }

    public DomainException WithValidationError(
        string field,
        string message,
        string? code = null,
        object? attemptedValue = null
    )
    {
        ValidationErrors.Add(
            new ValidationError
            {
                Field = field,
                Message = message,
                Code = code,
                AttemptedValue = attemptedValue,
            }
        );
        return this;
    }

    public virtual object ToProblemDetails()
    {
        return new
        {
            type = Type,
            title = Title,
            status = Status,
            detail = Detail,
            instance = Instance,
            timestamp = Timestamp,
            correlationId = CorrelationId,
            userId = UserId,
            tenantId = TenantId,
            extensions = Extensions,
            validationErrors = ValidationErrors,
        };
    }
}

public class ValidationError
{
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Code { get; set; }
    public object? AttemptedValue { get; set; }
}
