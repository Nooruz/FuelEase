using KIT.GasStation.Domain.Abstractions;
using KIT.GasStation.Domain.Exceptions;

namespace KIT.GasStation.Domain.ValueObjects;

/// <summary>
/// Объём топлива с единицей измерения.
/// </summary>
public sealed class Volume : ValueObject
{
    public decimal Quantity { get; }

    /// <summary>
    /// Единица измерения: "л" (литры), "м³" (кубометры) и т.д.
    /// </summary>
    public string Unit { get; }

    public static readonly string Litre = "л";

    private Volume() { Quantity = 0; Unit = Litre; }

    public Volume(decimal quantity, string unit = "л")
    {
        if (quantity < 0)
            throw new DomainException($"Объём не может быть отрицательным: {quantity}.");

        if (string.IsNullOrWhiteSpace(unit))
            throw new DomainException("Единица измерения не может быть пустой.");

        Quantity = quantity;
        Unit = unit;
    }

    public static Volume Zero(string unit = "л") => new(0, unit);

    public Volume Add(Volume other)
    {
        if (Unit != other.Unit)
            throw new DomainException($"Нельзя складывать {Unit} и {other.Unit}.");
        return new Volume(Quantity + other.Quantity, Unit);
    }

    public Volume Subtract(Volume other)
    {
        if (Unit != other.Unit)
            throw new DomainException($"Нельзя вычитать {Unit} и {other.Unit}.");
        if (Quantity < other.Quantity)
            throw new DomainException("Результат вычитания не может быть отрицательным.");
        return new Volume(Quantity - other.Quantity, Unit);
    }

    public override string ToString() => $"{Quantity:N3} {Unit}";

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Quantity;
        yield return Unit;
    }
}
