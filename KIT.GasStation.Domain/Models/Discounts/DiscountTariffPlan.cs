namespace KIT.GasStation.Domain.Models.Discounts;

/// <summary>
/// Тарифный план (диапазон сумм → процент скидки)
/// </summary>
public class DiscountTariffPlan : DomainObject
{
    /// <summary>Id скидки</summary>
    public int DiscountId { get; set; }

    /// <summary>Минимальная сумма покупки для применения скидки</summary>
    public decimal MinimumValue { get; set; }

    /// <summary>Максимальная сумма покупки (0 = без ограничения)</summary>
    public decimal MaximumValue { get; set; }

    /// <summary>Размер скидки в процентах</summary>
    public decimal DiscountValue { get; set; }

    public Discount? Discount { get; set; }
}
