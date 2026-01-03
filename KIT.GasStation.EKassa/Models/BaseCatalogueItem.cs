using System.Text.Json.Serialization;

namespace KIT.GasStation.EKassa.Models
{
    public sealed record BaseCatalogueItem
    {
        [JsonPropertyName("name")]
        public string Name { get; init; } = default!;

        [JsonPropertyName("code")]
        public int Code { get; init; }

        [JsonPropertyName("taxSystemCode")]
        public int TaxSystemCode { get; init; }

        [JsonPropertyName("vatCode")]
        public int VatCode { get; init; }

        [JsonPropertyName("stCashlessCode")]
        public int StCashlessCode { get; init; }

        [JsonPropertyName("stCashCode")]
        public int StCashCode { get; init; }

        [JsonPropertyName("modified")]
        public DateTimeOffset? Modified { get; init; }
    }
}
