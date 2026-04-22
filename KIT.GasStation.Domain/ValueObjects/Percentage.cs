using KIT.GasStation.Domain.Abstractions;
using KIT.GasStation.Domain.Exceptions;

namespace KIT.GasStation.Domain.ValueObjects;

/// <summary>
/// Процентное значение скидки или налога (0–100).
/// </summary>
public sealed class Percentage : ValueObject
{
    public decimal Value { get; }

    private Percentage() { Value = 0; }

    public Percentage(decimal value)
    {
        if (value < 0 || value > 100)
            throw new DomainException($"Процент должен быть от 0 до 100, получено: {value}.");

        Value = value;
    }

    public static Percentage Zero => new(0);
    public static Percentage Hundred => new(100);

    /// <summary>Применить скидку к сумме.</summary>
    public decimal ApplyDiscount(decimal amount)
        => Math.Round(amount * (1 - Value / 100), 2);

    /// <summary>Вычислить сумму скидки.</summary>
    public decimal CalculateDiscountAmount(decimal amount)
        => Math.Round(amount * Value / 100, 2);

    public override string ToString() => $"{Value:N2}%";

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
