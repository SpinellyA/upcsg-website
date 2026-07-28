using UpcsgWeb.Domain.Common;

namespace UpcsgWeb.Domain.ValueObjects;

/// <summary>
/// Money as a value object rather than a bare decimal, so amounts can't go negative,
/// can't silently carry sub-centavo precision, and can't be added across currencies.
/// </summary>
public sealed record Money
{
    public const string DefaultCurrency = "PHP";

    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Of(decimal amount, string currency = DefaultCurrency)
    {
        if (amount < 0)
        {
            throw new DomainException("Money cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new DomainException("Money requires a currency.");
        }

        // Round once, here, so totals can't drift by fractions of a centavo.
        return new Money(decimal.Round(amount, 2, MidpointRounding.ToEven), currency.ToUpperInvariant());
    }

    public static Money Zero(string currency = DefaultCurrency) => new(0m, currency);

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return Of(Amount + other.Amount, Currency);
    }

    public Money MultiplyBy(int quantity)
    {
        if (quantity < 0)
        {
            throw new DomainException("Quantity cannot be negative.");
        }

        return Of(Amount * quantity, Currency);
    }

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
        {
            throw new DomainException($"Cannot combine {Currency} with {other.Currency}.");
        }
    }

    public override string ToString() => $"{Currency} {Amount:0.00}";
}
