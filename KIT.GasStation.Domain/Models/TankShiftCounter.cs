namespace KIT.GasStation.Domain.Models;

/// <summary>
/// Счётчики резервуара по смене
/// </summary>
public class TankShiftCounter : DomainObject
{
    public int ShiftId { get; set; }
    public int TankId { get; set; }

    /// <summary>Остаток на начало смены (литры)</summary>
    public decimal BeginCount { get; set; }

    /// <summary>Остаток на конец смены (литры)</summary>
    public decimal EndCount { get; set; }

    public Shift? Shift { get; set; }
    public Tank? Tank { get; set; }
}
