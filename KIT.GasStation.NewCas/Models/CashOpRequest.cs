using System.Text.Json.Serialization;

namespace KIT.GasStation.NewCas.Models
{
    /// <summary>Запрос внесения/изъятия наличных (NewCas/Goodoo).</summary>
    public class CashOpRequest
    {
        /// <summary>Сумма в основных единицах (сомах).</summary>
        [JsonPropertyName("amount")]
        public string Amount { get; set; } = "0.00";
    }
}
