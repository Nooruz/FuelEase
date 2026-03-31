namespace KIT.GasStation.FuelDispenser.Models
{
    /// <summary>
    /// Тип налива топлива. По количеству или по объему.
    /// </summary>
    public enum FuelingStartMode
    {
        /// <summary>
        /// Налив по объему.
        /// </summary>
        ByAmount = 1,

        /// <summary>
        /// Налив по количеству.
        /// </summary>
        ByVolume = 2
    }
}
