using KIT.GasStation.Domain.Exceptions;

namespace KIT.GasStation.Domain.Models;

/// <summary>
/// История изменений цены топлива.
/// Создаётся автоматически методом <see cref="Fuel.Reprice"/> при каждой переоценке.
/// </summary>
public class FuelPriceHistory : DomainObject
{
    #region Persisted Properties

    /// <summary>Id топлива</summary>
    public int FuelId { get; set; }

    /// <summary>Цена до изменения</summary>
    public decimal OldPrice { get; set; }

    /// <summary>Цена после изменения</summary>
    public decimal NewPrice { get; set; }

    /// <summary>Дата и время переоценки (UTC)</summary>
    public DateTime ChangedAt { get; set; }

    /// <summary>ФИО сотрудника, выполнившего переоценку</summary>
    public string ChangedBy { get; set; } = string.Empty;

    /// <summary>Причина переоценки (необязательно)</summary>
    public string? Reason { get; set; }

    #endregion

    #region Navigation

    public Fuel? Fuel { get; set; }

    #endregion

    #region Computed

    /// <summary>Изменение цены (новая − старая)</summary>
    public decimal Delta => NewPrice - OldPrice;

    /// <summary>Направление изменения: +1 рост, -1 снижение, 0 без изменений</summary>
    public int Direction => Math.Sign(Delta);

    #endregion
}
