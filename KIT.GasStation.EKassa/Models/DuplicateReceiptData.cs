using System.Text.Json.Serialization;

namespace KIT.GasStation.EKassa.Models
{
    public sealed record DuplicateReceiptData
    {
        [JsonPropertyName("id")]
        public string Id { get; init; } = default!;

        [JsonPropertyName("fiscalNumber")]
        public string FiscalNumber { get; init; } = default!;

        [JsonPropertyName("link")]
        public string? Link { get; init; }

        [JsonPropertyName("txt")]
        public string? Txt { get; init; }

        [JsonPropertyName("html")]
        public string? Html { get; init; }
    }
}
