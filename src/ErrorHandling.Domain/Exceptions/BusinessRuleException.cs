namespace ErrorHandling.Domain.Exceptions;

public class BusinessRuleException : DomainException
{
    public string RuleName { get; }

    public BusinessRuleException(string ruleName, string message)
        : base(message, $"BUSINESS_RULE_{ruleName.ToUpperInvariant()}", 422)
    {
        RuleName = ruleName;
        WithExtension("ruleName", RuleName);
    }

    protected override string GetTitle() => "Business Rule Violation";
}

public class InvariantViolationException : DomainException
{
    public string InvariantName { get; }
    public object? CurrentValue { get; }
    public object? ExpectedValue { get; }

    public InvariantViolationException(
        string invariantName,
        string message,
        object? currentValue = null,
        object? expectedValue = null
    )
        : base(message, $"INVARIANT_{invariantName.ToUpperInvariant()}", 422)
    {
        InvariantName = invariantName;
        CurrentValue = currentValue;
        ExpectedValue = expectedValue;

        WithExtension("invariantName", InvariantName);
        if (currentValue != null)
            WithExtension("currentValue", currentValue);
        if (expectedValue != null)
            WithExtension("expectedValue", expectedValue);
    }

    protected override string GetTitle() => "Business Invariant Violation";
}

public class InvalidStateTransitionException : DomainException
{
    public string FromState { get; }
    public string ToState { get; }
    public string? EntityType { get; }

    public InvalidStateTransitionException(
        string fromState,
        string toState,
        string? entityType = null
    )
        : base(
            $"Cannot transition from '{fromState}' to '{toState}'"
                + (entityType != null ? $" for {entityType}" : ""),
            "INVALID_STATE_TRANSITION",
            422
        )
    {
        FromState = fromState;
        ToState = toState;
        EntityType = entityType ?? string.Empty;

        WithExtension("fromState", FromState);
        WithExtension("toState", ToState);
        if (entityType != null)
            WithExtension("entityType", EntityType);
    }

    protected override string GetTitle() => "Invalid State Transition";
}
