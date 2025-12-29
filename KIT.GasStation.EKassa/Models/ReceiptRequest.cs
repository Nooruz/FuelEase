using System.Text.Json.Serialization;

namespace KIT.GasStation.EKassa.Models
{
    public class ReceiptRequest
    {
        [JsonPropertyName("fiscal_number")]
        public string FiscalNumber { get; set; }

        [JsonPropertyName("operation")]
        public string Operation { get; set; } // INCOME, INCOME_RETURN, EXPENDITURE, EXPENDITURE_RETURN [4]

        [JsonPropertyName("cash")]
        public bool Cash { get; set; } // true - наличные, false - безналичные [7]

        [JsonPropertyName("received")]
        public string Received { get; set; } // Сумма в копейках [7]

        //[JsonPropertyName("goods")]
        //public List<ReceiptItem> Goods { get; set; }

        [JsonPropertyName("html")]
        public bool? Html { get; set; }

        [JsonPropertyName("css")]
        public bool? Css { get; set; }

        [JsonPropertyName("txt")]
        public bool? Txt { get; set; }

        [JsonPropertyName("originFdNumber")]
        public int? OriginFdNumber { get; set; } // Обязателен для возврата [8]
    }
}
