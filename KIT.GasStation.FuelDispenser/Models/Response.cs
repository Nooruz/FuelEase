namespace KIT.GasStation.FuelDispenser.Models
{
    /// <summary>
    /// Ответ от устройства на команду, отправленную сервером. Содержит имя группы, к которой относится ответ.
    /// </summary>
    public class Response
    {
        /// <summary>
        /// Название группы, к которой относится ответ.
        /// </summary>
        public string GroupName { get; set; } = string.Empty;
    }
}
