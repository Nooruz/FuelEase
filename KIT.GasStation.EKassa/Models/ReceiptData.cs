using System.Text.Json.Serialization;

namespace KIT.GasStation.EKassa.Models
{
    public sealed record ReceiptData
    {
        [JsonPropertyName("id")]
        public string Id { get; init; } = default!;

        [JsonPropertyName("fiscalNumber")]
        public string FiscalNumber { get; init; } = default!;

        [JsonPropertyName("fields")]
        public EkassaTagFields? Fields { get; init; }

        [JsonPropertyName("link")]
        public string? Link { get; init; }

        [JsonPropertyName("txt")]
        public string? Txt { get; init; }

        [JsonPropertyName("html")]
        public string? Html { get; init; }

        [JsonPropertyName("operatorResponse")]
        public EkassaOperatorResponse? OperatorResponse { get; init; }
    }
}
