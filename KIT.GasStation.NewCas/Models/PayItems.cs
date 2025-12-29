using System.Text.Json.Serialization;

namespace KIT.GasStation.NewCas.Models
{
    public class PayItems
    {
        [JsonPropertyName("payType")]
        public PayType PayType { get; set; }

        [JsonPropertyName("total")]
        public string Total { get; set; }
    }
}
