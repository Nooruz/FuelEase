using KIT.GasStation.Domain.Exceptions;
using System.ComponentModel.DataAnnotations;

namespace KIT.GasStation.Domain.Models;

/// <summary>
/// Подземный резервуар АЗС
/// </summary>
[Display(Name = "Резервуар")]
public class Tank : DomainObject
{
    #region Constructors

    public Tank() { }

    /// <summary>Копирующий конструктор</summary>
    public Tank(Tank source)
    {
        Id = source.Id;
        Name = source.Name;
        Number = source.Number;
        FuelId = source.FuelId;
        Size = source.Size;
        MinimumSize = source.MinimumSize;
        IsDeleted = source.IsDeleted;
        CreatedAt = source.CreatedAt;
        UpdatedAt = source.UpdatedAt;
        DeletedAt = source.DeletedAt;
    }

    #endregion

    #region Persisted Properties

    /// <summary>Наименование резервуара</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Номер резервуара (технологический)</summary>
    public int Number { get; set; }

    /// <summary>Id топлива</summary>
    public int FuelId { get; set; }

    /// <summary>Полный объём резервуара (литры)</summary>
    public decimal Size { get; set; }

    /// <summary>Мёртвый остаток — минимальный уровень (литры)</summary>
    public decimal MinimumSize { get; set; }

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

    public Fuel? Fuel { get; set; }
    public ICollection<FuelIntake> FuelIntakes { get; set; } = new List<FuelIntake>();
    public ICollection<FuelSale> FuelSales { get; set; } = new List<FuelSale>();
    public ICollection<Nozzle> Nozzle { get; set; } = new List<Nozzle>();
    public ICollection<TankShiftCounter> TankShiftCounters { get; set; } = new List<TankShiftCounter>();

    #endregion

    #region Business Methods

    /// <summary>
    /// Проверить, хватает ли топлива для отпуска указанного количества.
    /// </summary>
    public bool CanDispense(decimal requestedLitres, decimal currentLevel)
        => currentLevel - requestedLitres >= MinimumSize;

    /// <summary>Мягкое удаление</summary>
    public void Delete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }

    #endregion
}
