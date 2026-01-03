using System.Text.Json.Serialization;

namespace KIT.GasStation.EKassa.Models
{
    public sealed record CashOperationRequest
    {
        [JsonPropertyName("fiscal_number")]
        public string FiscalNumber { get; init; } = default!;

        /// <summary>5 – внесение, 6 – изъятие</summary>
        [JsonPropertyName("operation_type")]
        public int OperationType { get; init; } // 5 or 6

        /// <summary>Сумма в копейках.</summary>
        [JsonPropertyName("amount")]
        public int Amount { get; init; }

        [JsonPropertyName("html")] public bool? Html { get; init; }
        [JsonPropertyName("css")] public bool? Css { get; init; }
        [JsonPropertyName("txt")] public bool? Txt { get; init; }
        [JsonPropertyName("txt80")] public bool? Txt80 { get; init; }
    }
}
