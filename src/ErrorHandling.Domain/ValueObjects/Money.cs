using ErrorHandling.Domain.Exceptions;
using ErrorHandling.Domain.Results;

namespace ErrorHandling.Domain.ValueObjects;

public class Money : IEquatable<Money>, IComparable<Money>
{
    // Common ISO 4217 currency codes
    private static readonly HashSet<string> ValidCurrencyCodes = new()
    {
        "USD", "EUR", "GBP", "JPY", "CHF", "CAD", "AUD", "NZD",
        "CNY", "INR", "KRW", "SGD", "HKD", "NOK", "SEK", "DKK",
        "PLN", "CZK", "HUF", "RON", "BGN", "HRK", "RUB", "TRY",
        "BRL", "MXN", "ARS", "CLP", "COP", "PEN", "UYU", "ZAR"
    };

    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    // Exception-based factory
    public static Money Create(decimal amount, string currency = "USD")
    {
        if (amount < 0)
            throw new InvariantViolationException(
                "POSITIVE_AMOUNT",
                "Money amount cannot be negative",
                amount,
                ">= 0"
            );

        if (string.IsNullOrWhiteSpace(currency))
            throw new ValidationException("currency", "Currency cannot be empty");

        var upperCurrency = currency.ToUpperInvariant();
        if (!ValidCurrencyCodes.Contains(upperCurrency))
            throw new ValidationException("currency", $"Invalid currency code: {currency}. Must be a valid ISO 4217 code (e.g., USD, EUR, GBP).");

        return new Money(amount, upperCurrency);
    }

    // Result-based factory
    public static Result<Money> TryCreate(decimal amount, string currency = "USD")
    {
        if (amount < 0)
            return Result<Money>.Failure(
                new BusinessRuleError(
                    "POSITIVE_AMOUNT",
                    "Money amount cannot be negative"
                ).WithMetadata("amount", amount)
            );

        if (string.IsNullOrWhiteSpace(currency))
            return Result<Money>.Failure(Error.Validation("currency", "Currency cannot be empty"));

        var upperCurrency = currency.ToUpperInvariant();
        if (!ValidCurrencyCodes.Contains(upperCurrency))
            return Result<Money>.Failure(
                Error.Validation("currency", $"Invalid currency code: {currency}. Must be a valid ISO 4217 code (e.g., USD, EUR, GBP).")
            );

        return Result<Money>.Success(new Money(amount, upperCurrency));
    }

    public static Money Zero(string currency = "USD") => new(0, currency.ToUpperInvariant());

    public Money Add(Money? other)
    {
        if (other is null)
            throw new ArgumentNullException(nameof(other));

        if (Currency != other.Currency)
            throw new BusinessRuleException(
                "CURRENCY_MISMATCH",
                $"Cannot add money with different currencies: {Currency} and {other.Currency}"
            );

        return new Money(Amount + other.Amount, Currency);
    }

    public Result<Money> TryAdd(Money? other)
    {
        if (other is null)
            return Result<Money>.Failure("NULL_VALUE", "Cannot add null money");

        if (Currency != other.Currency)
            return Result<Money>.Failure(
                new BusinessRuleError(
                    "CURRENCY_MISMATCH",
                    $"Cannot add money with different currencies: {Currency} and {other.Currency}"
                )
            );

        return Result<Money>.Success(new Money(Amount + other.Amount, Currency));
    }

    public Money Subtract(Money? other)
    {
        if (other is null)
            throw new ArgumentNullException(nameof(other));

        if (Currency != other.Currency)
            throw new BusinessRuleException(
                "CURRENCY_MISMATCH",
                $"Cannot subtract money with different currencies: {Currency} and {other.Currency}"
            );

        if (Amount < other.Amount)
            throw new BusinessRuleException(
                "INSUFFICIENT_FUNDS",
                $"Cannot subtract {other.Amount} from {Amount}"
            );

        return new Money(Amount - other.Amount, Currency);
    }

    public Result<Money> TrySubtract(Money? other)
    {
        if (other is null)
            return Result<Money>.Failure("NULL_VALUE", "Cannot subtract null money");

        if (Currency != other.Currency)
            return Result<Money>.Failure(
                new BusinessRuleError(
                    "CURRENCY_MISMATCH",
                    $"Cannot subtract money with different currencies: {Currency} and {other.Currency}"
                )
            );

        if (Amount < other.Amount)
            return Result<Money>.Failure(
                new BusinessRuleError(
                    "INSUFFICIENT_FUNDS",
                    $"Cannot subtract {other.Amount} from {Amount}"
                )
                    .WithMetadata("currentAmount", Amount)
                    .WithMetadata("requestedAmount", other.Amount)
            );

        return Result<Money>.Success(new Money(Amount - other.Amount, Currency));
    }

    public bool Equals(Money? other)
    {
        if (other is null)
            return false;
        return Amount == other.Amount && Currency == other.Currency;
    }

    public override bool Equals(object? obj) => Equals(obj as Money);

    public override int GetHashCode() => HashCode.Combine(Amount, Currency);

    public int CompareTo(Money? other)
    {
        if (other is null)
            return 1;
        if (Currency != other.Currency)
            throw new InvalidOperationException(
                $"Cannot compare money with different currencies: {Currency} and {other.Currency}"
            );
        return Amount.CompareTo(other.Amount);
    }

    public override string ToString() => $"{Amount:F2} {Currency}";

    public static bool operator ==(Money? left, Money? right)
    {
        if (ReferenceEquals(left, right))
            return true;
        if (left is null || right is null)
            return false;
        return left.Equals(right);
    }

    public static bool operator !=(Money? left, Money? right) => !(left == right);

    public static bool operator <(Money? left, Money? right)
    {
        if (left is null)
            return right is not null;
        return left.CompareTo(right) < 0;
    }

    public static bool operator >(Money? left, Money? right)
    {
        if (right is null)
            return left is not null;
        if (left is null)
            return false;
        return left.CompareTo(right) > 0;
    }

    public static bool operator <=(Money? left, Money? right)
    {
        if (left is null)
            return true;
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >=(Money? left, Money? right)
    {
        if (right is null)
            return true;
        if (left is null)
            return false;
        return left.CompareTo(right) >= 0;
    }
}
