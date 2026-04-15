using System.Text.Json.Serialization;

namespace KIT.GasStation.NewCas.Models
{
    public class Discounts
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }
        [JsonPropertyName("amount")]
        public string? Amount { get; set; }
    }
}
