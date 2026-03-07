namespace KIT.GasStation.FuelDispenser.Models
{
    /// <summary>
    /// DTO, представляющий проанализированный ответ устройства.
    /// </summary>
    public class FuelingResponse : Response
    {
        public decimal Quantity { get; set; }
        public decimal Sum { get; set; }
    }
}
