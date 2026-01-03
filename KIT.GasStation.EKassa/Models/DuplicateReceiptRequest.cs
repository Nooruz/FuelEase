using System.Text.Json.Serialization;

namespace KIT.GasStation.EKassa.Models
{
    public sealed record DuplicateReceiptRequest
    {
        [JsonPropertyName("fiscal_number")]
        public string FiscalNumber { get; init; } = default!;

        [JsonPropertyName("fd_number")]
        public int FdNumber { get; init; }

        [JsonPropertyName("html")] public bool? Html { get; init; }
        [JsonPropertyName("css")] public bool? Css { get; init; }
        [JsonPropertyName("txt")] public bool? Txt { get; init; }
        [JsonPropertyName("txt80")] public bool? Txt80 { get; init; }
    }
}
