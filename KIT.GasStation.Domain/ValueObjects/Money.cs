using KIT.GasStation.Domain.Abstractions;
using KIT.GasStation.Domain.Exceptions;

namespace KIT.GasStation.Domain.ValueObjects;

/// <summary>
/// Денежная сумма с кодом валюты.
/// </summary>
public sealed class Money : ValueObject
{
    public decimal Amount { get; }

    /// <summary>
    /// ISO 4217 код валюты (например "UZS", "USD").
    /// </summary>
    public string Currency { get; }

    public static readonly string DefaultCurrency = "UZS";

    private Money() { Amount = 0; Currency = DefaultCurrency; }

    public Money(decimal amount, string currency = "UZS")
    {
        if (amount < 0)
            throw new DomainException($"Сумма не может быть отрицательной: {amount}.");

        if (string.IsNullOrWhiteSpace(currency))
            throw new DomainException("Код валюты не может быть пустым.");

        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    public static Money Zero(string currency = "UZS") => new(0, currency);

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new DomainException($"Нельзя складывать {Currency} и {other.Currency}.");
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        if (Currency != other.Currency)
            throw new DomainException($"Нельзя вычитать {Currency} и {other.Currency}.");
        if (Amount < other.Amount)
            throw new DomainException("Результат вычитания не может быть отрицательным.");
        return new Money(Amount - other.Amount, Currency);
    }

    public Money Multiply(decimal factor)
    {
        if (factor < 0)
            throw new DomainException("Множитель не может быть отрицательным.");
        return new Money(Math.Round(Amount * factor, 2), Currency);
    }

    public static Money FromDecimal(decimal amount, string currency = "UZS") => new(amount, currency);

    public override string ToString() => $"{Amount:N2} {Currency}";

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
