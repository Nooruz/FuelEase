namespace KIT.GasStation.FuelDispenser.Models
{
    /// <summary>
    /// Продолжить заправку, который сервер отправляет воркеру/оборудованию.
    /// </summary>
    public class ResumeFuelingRequest : Response
    {
        public decimal Quantity { get; set; }
        public decimal Sum { get; set; }
    }
}
