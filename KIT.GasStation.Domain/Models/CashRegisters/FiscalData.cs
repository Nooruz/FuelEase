namespace KIT.GasStation.Domain.Models;

/// <summary>
/// Фискальные данные чека
/// </summary>
public class FiscalData : DomainObject
{
    /// <summary>Фискальный документ (ФД)</summary>
    public int? FiscalDocument { get; set; }

    /// <summary>Фискальный признак документа (ФМ)</summary>
    public string? FiscalModule { get; set; }

    /// <summary>Чек продажи (base64 / текст)</summary>
    public string? Check { get; set; }

    /// <summary>Чек возврата (base64 / текст)</summary>
    public string? ReturnCheck { get; set; }

    /// <summary>Регистрационный номер ККМ</summary>
    public string? RegistrationNumber { get; set; }

    /// <summary>Id продажи топлива</summary>
    public int FuelSaleId { get; set; }

    /// <summary>Продажа топлива</summary>
    public FuelSale FuelSale { get; set; } = null!;
}
