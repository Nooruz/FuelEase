using System.ComponentModel.DataAnnotations;

namespace KIT.GasStation.Domain.Models;

/// <summary>
/// Запись в панели событий смены
/// </summary>
public class EventPanel : DomainObject
{
    /// <summary>Текст сообщения</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Дата и время события</summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>Id смены</summary>
    public int ShiftId { get; set; }

    /// <summary>Тип события</summary>
    public EventPanelType Type { get; set; }

    /// <summary>Сущность, к которой относится событие</summary>
    public EventEntity EventEntity { get; set; }

    /// <summary>Id связанной сущности</summary>
    public int EntityId { get; set; }

    public Shift? Shift { get; set; }
}

public enum EventPanelType
{
    None,

    [Display(Name = "Информация")]
    Information,

    [Display(Name = "Ошибка")]
    Error
}

public enum EventEntity
{
    Shift,
    CashRegister,
    FuelSale,
    Fuel,
    Nozzle,
    Tank,
    UnregisteredSale,
    User,
    Discount,
    CashOperation
}
