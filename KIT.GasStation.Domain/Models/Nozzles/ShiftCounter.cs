namespace KIT.GasStation.Domain.Models;

/// <summary>
/// Счётчики ТРК по смене
/// </summary>
public class ShiftCounter : DomainObject
{
    public int ShiftId { get; set; }
    public int NozzleId { get; set; }

    /// <summary>Показание счётчика ТРК на начало смены</summary>
    public decimal BeginNozzleCounter { get; set; }

    /// <summary>Показание счётчика ТРК на конец смены</summary>
    public decimal EndNozzleCounter { get; set; }

    /// <summary>Показание счётчика СУ на начало смены</summary>
    public decimal BeginSaleCounter { get; set; }

    /// <summary>Показание счётчика СУ на конец смены</summary>
    public decimal EndSaleCounter { get; set; }

    public Shift? Shift { get; set; }
    public Nozzle? Nozzle { get; set; }
}
