using System.Text.RegularExpressions;
using ErrorHandling.Domain.Exceptions;
using ErrorHandling.Domain.Results;

namespace ErrorHandling.Domain.ValueObjects;

public class Email : IEquatable<Email>
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled
    );

    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    // Exception-based factory
    public static Email Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ValidationException("email", "Email cannot be empty");

        if (!EmailRegex.IsMatch(value))
            throw new ValidationException("email", $"'{value}' is not a valid email address");

        return new Email(value.ToLowerInvariant());
    }

    // Result-based factory
    public static Result<Email> TryCreate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<Email>.Failure(Error.Validation("email", "Email cannot be empty"));

        if (!EmailRegex.IsMatch(value))
            return Result<Email>.Failure(
                Error.Validation("email", $"'{value}' is not a valid email address")
            );

        return Result<Email>.Success(new Email(value.ToLowerInvariant()));
    }

    public bool Equals(Email? other)
    {
        if (other is null)
            return false;
        return Value == other.Value;
    }

    public override bool Equals(object? obj) => Equals(obj as Email);

    public override int GetHashCode() => Value?.GetHashCode() ?? 0;

    public override string ToString() => Value;

    public static implicit operator string?(Email? email) => email?.Value;
}
