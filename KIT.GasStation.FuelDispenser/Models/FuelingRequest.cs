namespace KIT.GasStation.FuelDispenser.Models
{
    /// <summary>
    /// Запрос на заправку, который сервер отправляет воркеру/оборудованию.
    /// </summary>
    public class FuelingRequest : Response
    {
        /// <summary>
        /// Тип заправки, который определяет, по какому параметру начинать заправку: по количеству, по сумме или по времени.
        /// </summary>
        public FuelingStartMode FuelingStartMode { get; set; }

        /// <summary>
        /// Количество или сумма, по которому нужно начать заправку. Если тип заправки - по количеству, то это количество в литрах. Если тип заправки - по сумме, то это сумма в соммах.
        /// </summary>
        public decimal Quantity { get; set; }
        public decimal Sum { get; set; }
    }
}
