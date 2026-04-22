using KIT.GasStation.Domain.Exceptions;
using System.ComponentModel.DataAnnotations;

namespace KIT.GasStation.Domain.Models;

/// <summary>
/// Кассовая операция — внесение, изъятие или инкассация наличных.
/// Фиксирует каждое движение наличных вне продажи топлива.
/// </summary>
public class CashOperation : DomainObject
{
    #region Persisted Properties

    /// <summary>Id смены</summary>
    public int ShiftId { get; set; }

    /// <summary>Тип операции</summary>
    [EnumDataType(typeof(CashOperationType))]
    public CashOperationType Type { get; set; }

    /// <summary>Сумма операции (сум)</summary>
    public decimal Amount { get; set; }

    /// <summary>ФИО кассира, выполнившего операцию</summary>
    public string CashierName { get; set; } = string.Empty;

    /// <summary>Описание / примечание (необязательно)</summary>
    public string? Description { get; set; }

    /// <summary>Номер фискального документа ККМ (если был распечатан чек)</summary>
    public int? FiscalDocument { get; set; }

    /// <summary>Дата и время операции</summary>
    public DateTime CreatedAt { get; set; }

    #endregion

    #region Navigation

    public Shift? Shift { get; set; }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Создать операцию «Внесение в кассу».
    /// </summary>
    public static CashOperation CreateDeposit(int shiftId, decimal amount, string cashierName, string? description = null)
    {
        ValidateAmount(amount);
        return new CashOperation
        {
            ShiftId = shiftId,
            Type = CashOperationType.Deposit,
            Amount = amount,
            CashierName = cashierName,
            Description = description,
            CreatedAt = DateTime.Now
        };
    }

    /// <summary>
    /// Создать операцию «Изъятие из кассы».
    /// </summary>
    public static CashOperation CreateWithdrawal(int shiftId, decimal amount, string cashierName, string? description = null)
    {
        ValidateAmount(amount);
        return new CashOperation
        {
            ShiftId = shiftId,
            Type = CashOperationType.Withdrawal,
            Amount = amount,
            CashierName = cashierName,
            Description = description,
            CreatedAt = DateTime.Now
        };
    }

    /// <summary>
    /// Создать операцию «Инкассация».
    /// </summary>
    public static CashOperation CreateCollection(int shiftId, decimal amount, string cashierName, string? description = null)
    {
        ValidateAmount(amount);
        return new CashOperation
        {
            ShiftId = shiftId,
            Type = CashOperationType.Collection,
            Amount = amount,
            CashierName = cashierName,
            Description = description,
            CreatedAt = DateTime.Now
        };
    }

    private static void ValidateAmount(decimal amount)
    {
        if (amount <= 0)
            throw new DomainException($"Сумма кассовой операции должна быть положительной, получено: {amount}.");
    }

    #endregion
}

/// <summary>Тип кассовой операции</summary>
public enum CashOperationType
{
    /// <summary>Внесение наличных в кассу</summary>
    [Display(Name = "Внесение")]
    Deposit = 1,

    /// <summary>Изъятие наличных из кассы</summary>
    [Display(Name = "Изъятие")]
    Withdrawal = 2,

    /// <summary>Инкассация (передача наличных инкассатору)</summary>
    [Display(Name = "Инкассация")]
    Collection = 3
}
