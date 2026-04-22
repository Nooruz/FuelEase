using KIT.GasStation.Domain.Abstractions;
using KIT.GasStation.Domain.Exceptions;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KIT.GasStation.Domain.Models;

/// <summary>
/// Кассовая смена
/// </summary>
public class Shift : AggregateRoot
{
    #region Persisted Properties

    /// <summary>Id пользователя, открывшего смену</summary>
    public int UserId { get; set; }

    /// <summary>Дата и время открытия смены</summary>
    public DateTime OpeningDate { get; set; }

    /// <summary>Дата и время закрытия смены (null — смена открыта)</summary>
    public DateTime? ClosedDate { get; set; }

    /// <summary>ФИО сотрудника, открывшего смену</summary>
    public string OpenedBy { get; set; } = string.Empty;

    /// <summary>ФИО сотрудника, закрывшего смену</summary>
    public string? ClosedBy { get; set; }

    /// <summary>Остаток наличных в кассе на начало смены</summary>
    public decimal OpeningCashBalance { get; set; }

    /// <summary>Остаток наличных в кассе на конец смены (заполняется при закрытии)</summary>
    public decimal? ClosingCashBalance { get; set; }

    #endregion

    #region Navigation

    public User? User { get; set; }
    public ICollection<FuelSale> FuelSales { get; set; } = new List<FuelSale>();
    public ICollection<UnregisteredSale> UnregisteredSales { get; set; } = new List<UnregisteredSale>();
    public ICollection<EventPanel> EventsPanel { get; set; } = new List<EventPanel>();
    public ICollection<ShiftCounter> NozzleShiftCounters { get; set; } = new List<ShiftCounter>();
    public ICollection<TankShiftCounter> TankShiftCounters { get; set; } = new List<TankShiftCounter>();
    public ICollection<FuelIntake> FuelIntakes { get; set; } = new List<FuelIntake>();
    public ICollection<CashOperation> CashOperations { get; set; } = new List<CashOperation>();

    #endregion

    #region Computed (not persisted)

    [NotMapped]
    public ShiftState ShiftState
    {
        get
        {
            if (Id == 0) return ShiftState.None;
            if (ClosedDate == null)
            {
                return (DateTime.Now - OpeningDate).TotalHours < 24
                    ? ShiftState.Open
                    : ShiftState.Exceeded24Hours;
            }
            return ShiftState.Closed;
        }
    }

    /// <summary>Итоговая выручка по завершённым продажам</summary>
    [NotMapped]
    public decimal Sum =>
        FuelSales?
            .Where(f => f.FuelSaleStatus == FuelSaleStatus.Completed)
            .Sum(f => f.ReceivedSum) ?? 0;

    #endregion

    #region Business Methods

    /// <summary>
    /// Открыть смену.
    /// </summary>
    public void Open(int userId, string openedBy, decimal openingCashBalance = 0)
    {
        if (ShiftState == ShiftState.Open)
            throw new DomainException("Смена уже открыта.");

        if (string.IsNullOrWhiteSpace(openedBy))
            throw new DomainException("Необходимо указать ФИО открывшего смену.");

        UserId = userId;
        OpenedBy = openedBy;
        OpeningDate = DateTime.Now;
        OpeningCashBalance = openingCashBalance;
        ClosedDate = null;
        ClosedBy = null;
        ClosingCashBalance = null;
    }

    /// <summary>
    /// Закрыть смену.
    /// </summary>
    public void Close(string closedBy, decimal closingCashBalance = 0)
    {
        if (ShiftState == ShiftState.Closed)
            throw new DomainException("Смена уже закрыта.");

        if (string.IsNullOrWhiteSpace(closedBy))
            throw new DomainException("Необходимо указать ФИО закрывшего смену.");

        ClosedDate = DateTime.Now;
        ClosedBy = closedBy;
        ClosingCashBalance = closingCashBalance;
    }

    /// <summary>
    /// Обновить навигационные свойства после перезагрузки из БД (используется в сервисном слое).
    /// </summary>
    public void SyncFrom(Shift updated)
    {
        ClosedDate = updated.ClosedDate;
        ClosedBy = updated.ClosedBy;
        ClosingCashBalance = updated.ClosingCashBalance;
        FuelSales = updated.FuelSales;
    }

    #endregion
}

/// <summary>Состояние смены</summary>
public enum ShiftState
{
    None,

    [Display(Name = "Открыта")]
    Open,

    [Display(Name = "Закрыта")]
    Closed,

    [Display(Name = "Превысила 24 часа")]
    Exceeded24Hours
}
