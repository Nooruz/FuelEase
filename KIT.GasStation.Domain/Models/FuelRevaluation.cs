namespace KIT.GasStation.Domain.Models;

/// <summary>
/// Запись переоценки топлива (старая цена → новая цена).
/// Устарела в пользу <see cref="FuelPriceHistory"/> — используйте FuelPriceHistory для новых записей.
/// </summary>
public class FuelRevaluation : DomainObject
{
    /// <summary>Дата переоценки</summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>Id топлива</summary>
    public int FuelId { get; set; }

    /// <summary>Новая цена</summary>
    public decimal NewPrice { get; set; }

    /// <summary>Старая цена</summary>
    public decimal OldPrice { get; set; }

    public Fuel? Fuel { get; set; }
}
