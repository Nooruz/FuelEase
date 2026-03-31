namespace KIT.GasStation.FuelDispenser.Models
{
    /// <summary>
    /// Продолжить заправку, который сервер отправляет воркеру/оборудованию.
    /// </summary>
    public class ResumeFuelingRequest : Response
    {
        /// <summary>
        /// Продолжить с каким количеством или суммой. Если тип заправки - по количеству, то это количество в литрах. Если тип заправки - по сумме, то это сумма в соммах.
        /// </summary>
        public decimal Value { get; set; }
    }
}
