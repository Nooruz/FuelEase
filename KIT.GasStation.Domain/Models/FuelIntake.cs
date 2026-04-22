namespace KIT.GasStation.Domain.Models;

/// <summary>
/// Приём топлива (поставка)
/// </summary>
public class FuelIntake : DomainObject
{
    /// <summary>Дата и время приёма</summary>
    public DateTime CreateDate { get; set; }

    /// <summary>Номер накладной / документа</summary>
    public string? Number { get; set; }

    /// <summary>Id резервуара</summary>
    public int TankId { get; set; }

    /// <summary>Id смены</summary>
    public int ShiftId { get; set; }

    /// <summary>Принятое количество (литры)</summary>
    public decimal Quantity { get; set; }

    /// <summary>Поставщик / перевозчик (необязательно)</summary>
    public string? SupplierName { get; set; }

    /// <summary>Номер ТЦ (топливозаправщика / трактора-цистерны)</summary>
    public string? TruckNumber { get; set; }

    /// <summary>Закупочная цена за литр</summary>
    public decimal PricePerLitre { get; set; }

    public Tank? Tank { get; set; }
    public Shift? Shift { get; set; }
}
