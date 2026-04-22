using KIT.GasStation.Domain.Exceptions;
using KIT.GasStation.Domain.Models.Discounts;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KIT.GasStation.Domain.Models;

/// <summary>
/// Топливо (справочник)
/// </summary>
[Display(Name = "Топливо")]
public class Fuel : DomainObject
{
    #region Constructors

    public Fuel() { }

    /// <summary>Копирующий конструктор</summary>
    public Fuel(Fuel source)
    {
        Id = source.Id;
        Name = source.Name;
        Price = source.Price;
        UnitOfMeasurementId = source.UnitOfMeasurementId;
        TNVED = source.TNVED;
        ValueAddedTax = source.ValueAddedTax;
        SalesTax = source.SalesTax;
        ColorHex = source.ColorHex;
        IsDeleted = source.IsDeleted;
        CreatedAt = source.CreatedAt;
        UpdatedAt = source.UpdatedAt;
        DeletedAt = source.DeletedAt;
    }

    #endregion

    #region Persisted Properties

    /// <summary>Наименование</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Текущая цена (сум/литр)</summary>
    public decimal Price { get; set; }

    /// <summary>Id единицы измерения</summary>
    public int UnitOfMeasurementId { get; set; }

    /// <summary>Код ТН ВЭД</summary>
    public string? TNVED { get; set; }

    /// <summary>НДС включён в цену</summary>
    public bool ValueAddedTax { get; set; }

    /// <summary>Ставка НСП (%)</summary>
    public decimal SalesTax { get; set; }

    /// <summary>Цвет топлива в UI (#RRGGBB)</summary>
    public string ColorHex { get; set; } = "#808080";

    /// <summary>Признак мягкого удаления</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Дата создания записи</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Дата последнего изменения</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Дата мягкого удаления</summary>
    public DateTime? DeletedAt { get; set; }

    #endregion

    #region Navigation

    public UnitOfMeasurement? UnitOfMeasurement { get; set; }
    public ICollection<Tank> Tanks { get; set; } = new List<Tank>();
    public ICollection<FuelRevaluation> FuelRevaluations { get; set; } = new List<FuelRevaluation>();
    public ICollection<DiscountFuel> DiscountFuels { get; set; } = new List<DiscountFuel>();
    public ICollection<FuelPriceHistory> PriceHistory { get; set; } = new List<FuelPriceHistory>();

    #endregion

    #region Business Methods

    /// <summary>
    /// Изменить цену топлива. Создаёт запись в истории цен.
    /// </summary>
    /// <param name="newPrice">Новая цена (сум/литр)</param>
    /// <param name="changedBy">ФИО сотрудника, выполнившего переоценку</param>
    public FuelPriceHistory Reprice(decimal newPrice, string changedBy)
    {
        if (newPrice <= 0)
            throw new DomainException($"Цена должна быть положительной, получено: {newPrice}.");

        if (string.IsNullOrWhiteSpace(changedBy))
            throw new DomainException("Необходимо указать сотрудника, выполнившего переоценку.");

        var history = new FuelPriceHistory
        {
            FuelId = Id,
            OldPrice = Price,
            NewPrice = newPrice,
            ChangedAt = DateTime.UtcNow,
            ChangedBy = changedBy
        };

        Price = newPrice;
        UpdatedAt = DateTime.UtcNow;

        return history;
    }

    /// <summary>Мягкое удаление</summary>
    public void Delete(string deletedBy)
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }

    #endregion
}
