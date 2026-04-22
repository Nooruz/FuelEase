using KIT.GasStation.Domain.Abstractions;
using KIT.GasStation.Domain.Exceptions;
using System.ComponentModel.DataAnnotations;

namespace KIT.GasStation.Domain.Models;

/// <summary>
/// Продажа топлива (отпуск через ТРК)
/// </summary>
public class FuelSale : AggregateRoot
{
    #region Persisted Properties

    /// <summary>Id резервуара</summary>
    public int TankId { get; set; }

    /// <summary>Id ТРК (пистолета)</summary>
    public int NozzleId { get; set; }

    /// <summary>Id смены</summary>
    public int ShiftId { get; set; }

    /// <summary>Тип оплаты</summary>
    public PaymentType PaymentType { get; set; }

    /// <summary>Тип операции (продажа / возврат / корректировка)</summary>
    public OperationType OperationType { get; set; }

    /// <summary>Дата и время создания заявки</summary>
    public DateTime CreateDate { get; set; }

    /// <summary>Цена (сум/литр на момент продажи)</summary>
    public decimal Price { get; set; }

    /// <summary>Заказанная сумма</summary>
    public decimal Sum { get; set; }

    /// <summary>Фактически полученная сумма</summary>
    public decimal ReceivedSum { get; set; }

    /// <summary>Заказанное количество (литры)</summary>
    public decimal Quantity { get; set; }

    /// <summary>Фактически отпущенное количество (литры)</summary>
    public decimal ReceivedQuantity { get; set; }

    /// <summary>Показание счётчика ТРК на конец заправки</summary>
    public decimal ReceivedCount { get; set; }

    /// <summary>Сумма, переданная клиентом</summary>
    public decimal? CustomerSum { get; set; }

    /// <summary>Сдача клиенту</summary>
    public decimal? ChangeSum { get; set; }

    /// <summary>Режим — «залить на сумму» (true) или «залить литры» (false)</summary>
    public bool IsForSum { get; set; }

    /// <summary>Статус продажи</summary>
    [EnumDataType(typeof(FuelSaleStatus))]
    public FuelSaleStatus FuelSaleStatus { get; set; }

    #endregion

    #region Navigation

    public Tank? Tank { get; set; }
    public Shift? Shift { get; set; }
    public DiscountSale? DiscountSale { get; set; }
    public FiscalData? FiscalData { get; set; }
    public Nozzle? Nozzle { get; set; }

    #endregion

    #region Business Methods

    /// <summary>
    /// Завершить продажу — зафиксировать фактически отпущенное топливо.
    /// </summary>
    public void Complete(decimal receivedSum, decimal receivedQuantity, decimal receivedCount)
    {
        if (FuelSaleStatus == FuelSaleStatus.Completed)
            throw new DomainException("Продажа уже завершена.");

        ReceivedSum = receivedSum;
        ReceivedQuantity = receivedQuantity;
        ReceivedCount = receivedCount;
        FuelSaleStatus = FuelSaleStatus.Completed;
    }

    /// <summary>
    /// Пометить как «обработано» (фискальный чек пробит).
    /// </summary>
    public void MarkProcessed()
    {
        if (FuelSaleStatus == FuelSaleStatus.Processed)
            throw new DomainException("Продажа уже помечена как обработанная.");

        FuelSaleStatus = FuelSaleStatus.Processed;
    }

    /// <summary>
    /// Начать обработку (статус InProcessed — отпуск идёт).
    /// </summary>
    public void StartProcessing()
    {
        FuelSaleStatus = FuelSaleStatus.InProcessed;
    }

    /// <summary>
    /// Отменить / аннулировать (Uncompleted).
    /// </summary>
    public void Cancel()
    {
        if (FuelSaleStatus == FuelSaleStatus.Completed)
            throw new DomainException("Завершённую продажу нельзя аннулировать — используйте возврат.");

        FuelSaleStatus = FuelSaleStatus.Uncompleted;
    }

    /// <summary>
    /// Создать копию для операции возврата.
    /// </summary>
    public FuelSale Clone() => new()
    {
        TankId = TankId,
        NozzleId = NozzleId,
        ShiftId = ShiftId,
        PaymentType = PaymentType,
        OperationType = OperationType,
        CreateDate = CreateDate,
        Price = Price,
        Sum = Sum,
        ReceivedSum = ReceivedSum,
        Quantity = Quantity,
        ReceivedQuantity = ReceivedQuantity,
        ReceivedCount = ReceivedCount,
        CustomerSum = CustomerSum,
        ChangeSum = ChangeSum,
        IsForSum = IsForSum,
        FuelSaleStatus = FuelSaleStatus,
        // Навигационные свойства — null, чтобы EF не дублировал трекинг
        Tank = null,
        Shift = null,
        DiscountSale = null,
        FiscalData = null,
        Nozzle = null
    };

    #endregion
}

/// <summary>Статус продажи</summary>
public enum FuelSaleStatus
{
    None,

    [Display(Name = "Обрабатывается")]
    InProcessed,

    [Display(Name = "Обработана")]
    Processed,

    [Display(Name = "Завершена")]
    Completed,

    [Display(Name = "Незавершена")]
    Uncompleted
}

/// <summary>Тип оплаты</summary>
public enum PaymentType
{
    None,

    [Display(Name = "Наличными")]
    Cash,

    [Display(Name = "Безналичными")]
    Cashless,

    [Display(Name = "Ведомость")]
    Statement,

    [Display(Name = "Талон")]
    Ticket,

    [Display(Name = "Дисконтная карта")]
    DiscountCard,

    [Display(Name = "Топливная карта")]
    FuelCard,

    [Display(Name = "Другое")]
    Other
}

/// <summary>Тип операции</summary>
public enum OperationType
{
    [Display(Name = "Продажа")]
    Sale,

    [Display(Name = "Возврат")]
    Return,

    [Display(Name = "Корректировка")]
    Correction
}
