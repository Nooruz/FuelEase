using KIT.GasStation.Domain.Exceptions;
using KIT.GasStation.Domain.Models.Discounts;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KIT.GasStation.Domain.Models;

/// <summary>
/// Скидочная акция с набором тарифных планов и применяемых топлив
/// </summary>
[Display(Name = "Скидка")]
public class Discount : DomainObject
{
    /// <summary>Наименование акции</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Дата начала действия скидки</summary>
    public DateTime StartDate { get; set; }

    /// <summary>Дата окончания действия скидки</summary>
    public DateTime EndDate { get; set; }

    /// <summary>Тарифные планы (диапазоны сумм → процент скидки)</summary>
    public ICollection<DiscountTariffPlan> DiscountTariffPlans { get; set; } = new List<DiscountTariffPlan>();

    /// <summary>Топлива, к которым применяется скидка</summary>
    public ICollection<DiscountFuel> DiscountFuels { get; set; } = new List<DiscountFuel>();

    /// <summary>Продажи, к которым была применена эта скидка</summary>
    public ICollection<DiscountSale> DiscountSales { get; set; } = new List<DiscountSale>();

    // ── Бизнес-методы ─────────────────────────────────────────────────────────

    /// <summary>Проверить, активна ли скидка на указанную дату.</summary>
    public bool IsActiveOn(DateTime date) => date >= StartDate && date <= EndDate;

    /// <summary>Продлить скидку до новой даты окончания.</summary>
    public void Extend(DateTime newEndDate)
    {
        if (newEndDate <= StartDate)
            throw new DomainException("Дата окончания должна быть позже даты начала.");
        EndDate = newEndDate;
    }
}
