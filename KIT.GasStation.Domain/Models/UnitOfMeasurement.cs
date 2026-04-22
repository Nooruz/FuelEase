namespace KIT.GasStation.Domain.Models;

/// <summary>
/// Единица измерения
/// </summary>
public class UnitOfMeasurement : DomainObject
{
    /// <summary>Наименование</summary>
    public string Name { get; set; } = string.Empty;

    public ICollection<Fuel> Fuels { get; set; } = new List<Fuel>();
}
