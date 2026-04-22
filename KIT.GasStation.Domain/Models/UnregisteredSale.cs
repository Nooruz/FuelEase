using System.ComponentModel.DataAnnotations;

namespace KIT.GasStation.Domain.Models;

/// <summary>
/// Незарегистрированный отпуск топлива (обнаружен по показаниям ТРК без чека)
/// </summary>
[Display(Name = "Незарегистрированная продажа")]
public class UnregisteredSale : DomainObject
{
    /// <summary>Id пистолета (ТРК)</summary>
    public int NozzleId { get; set; }

    /// <summary>Id смены</summary>
    public int ShiftId { get; set; }

    /// <summary>Дата и время обнаружения</summary>
    public DateTime CreateDate { get; set; }

    /// <summary>Сумма (расчётная)</summary>
    public decimal Sum { get; set; }

    /// <summary>Количество (литры)</summary>
    public decimal Quantity { get; set; }

    [EnumDataType(typeof(UnregisteredSaleState))]
    public UnregisteredSaleState State { get; set; }

    public Nozzle? Nozzle { get; set; }
    public Shift? Shift { get; set; }
}

/// <summary>Состояние незарегистрированной продажи</summary>
public enum UnregisteredSaleState
{
    None,

    [Display(Name = "Ожидание")]
    Waiting,

    [Display(Name = "Зарегистрировано как продажа")]
    Registered,

    [Display(Name = "Зарегистрировано как обратка (перекачка)")]
    Returned,

    [Display(Name = "Удалено")]
    Deleted
}
