using System.Text.Json.Serialization;

namespace KIT.GasStation.EKassa.Models
{
    public class ShiftOpen
    {

        [JsonPropertyName("fiscal_number")]
        public string FiscalNumber { get; set; } = "";

        [JsonPropertyName("html")]
        public bool Html { get; set; }

        [JsonPropertyName("css")]
        public bool Css { get; set; }

        [JsonPropertyName("txt")]
        public bool Txt { get; set; }

        [JsonPropertyName("txt80")]
        public bool Txt80 { get; set; }
    }
}
