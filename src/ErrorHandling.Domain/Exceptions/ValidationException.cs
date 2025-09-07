namespace ErrorHandling.Domain.Exceptions;

public class ValidationException : DomainException
{
    public ValidationException(string message)
        : base(message, "VALIDATION_ERROR", 400) { }

    public ValidationException(string field, string message)
        : base(message, "VALIDATION_ERROR", 400)
    {
        WithValidationError(field, message);
    }

    public ValidationException(IEnumerable<ValidationError> errors)
        : base("One or more validation errors occurred", "VALIDATION_ERROR", 400)
    {
        foreach (var error in errors)
        {
            ValidationErrors.Add(error);
        }
    }

    public ValidationException(Dictionary<string, string> fieldErrors)
        : base("One or more validation errors occurred", "VALIDATION_ERROR", 400)
    {
        foreach (var error in fieldErrors)
        {
            WithValidationError(error.Key, error.Value);
        }
    }

    protected override string GetTitle() => "Validation Error";

    public override string Message
    {
        get
        {
            if (ValidationErrors.Any())
            {
                var errors = string.Join(
                    "; ",
                    ValidationErrors.Select(e => $"{e.Field}: {e.Message}")
                );
                return $"{base.Message} - {errors}";
            }
            return base.Message;
        }
    }
}

public class DuplicateEntityException : DomainException
{
    public string EntityType { get; }
    public string DuplicateField { get; }
    public object DuplicateValue { get; }

    public DuplicateEntityException(string entityType, string duplicateField, object duplicateValue)
        : base(
            $"{entityType} with {duplicateField} '{duplicateValue}' already exists",
            "DUPLICATE_ENTITY",
            409
        )
    {
        EntityType = entityType;
        DuplicateField = duplicateField;
        DuplicateValue = duplicateValue;

        WithExtension("entityType", EntityType);
        WithExtension("duplicateField", DuplicateField);
        WithExtension("duplicateValue", DuplicateValue);
    }

    protected override string GetTitle() => "Duplicate Entity";
}
