namespace KIT.GasStation.Domain.Models;

/// <summary>
/// Применённая скидка к продаже
/// </summary>
public class DiscountSale : DomainObject
{
    public int FuelSaleId { get; set; }

    /// <summary>Id скидки</summary>
    public int DiscountId { get; set; }

    /// <summary>Цена после скидки</summary>
    public decimal DiscountPrice { get; set; }

    /// <summary>Сэкономленная сумма</summary>
    public decimal DiscountSum { get; set; }

    /// <summary>Количество топлива со скидкой</summary>
    public decimal DiscountQuantity { get; set; }

    public FuelSale? FuelSale { get; set; }
    public Discount? Discount { get; set; }
}
