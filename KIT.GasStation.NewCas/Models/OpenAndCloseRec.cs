using System.Text.Json.Serialization;

namespace KIT.GasStation.NewCas.Models
{
    public class OpenAndCloseRec
    {
        [JsonPropertyName("recType")]
        public RecType RecType { get; set; }

        [JsonPropertyName("cashierName")]
        public string CashierName { get; set; }

        [JsonPropertyName("goods")]
        public Goods[] Goods { get; set; }

        [JsonPropertyName("payItems")]
        public PayItems[] PayItems { get; set; }

        [JsonPropertyName("printToBitmaps")]
        public bool PrintToBitmaps { get; set; }

        [JsonPropertyName("sourceFMNumber")]
        public string? SourceFMNumber { get; set; }

        [JsonPropertyName("sourceFDNumber")]
        public int? SourceFDNumber { get; set; }

        [JsonPropertyName("discounts")]
        public Discounts[]? Discounts { get; set; }
    }
}
