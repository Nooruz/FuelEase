using System.Text.Json.Serialization;

namespace KIT.GasStation.NewCas.Models
{
    public class Discounts
    {
        [JsonPropertyName("amount")]
        public string? Amount { get; set; }
    }
}
