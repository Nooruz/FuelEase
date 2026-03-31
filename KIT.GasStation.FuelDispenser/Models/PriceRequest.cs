namespace KIT.GasStation.FuelDispenser.Models
{
    /// <summary>
    /// Запрос на установку цены по одной группе.
    /// </summary>
    public class PriceRequest : Response
    {
        /// <summary>
        /// Цена, которую нужно установить по группе.
        /// </summary>
        public decimal Value { get; set; }
    }
}
