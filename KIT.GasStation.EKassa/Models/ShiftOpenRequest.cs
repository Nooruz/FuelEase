using System.Text.Json.Serialization;

namespace KIT.GasStation.EKassa.Models
{
    public sealed record ShiftOpenRequest
    {
        [JsonPropertyName("fiscal_number")]
        public string FiscalNumber { get; init; } = default!;

        // Доп. форматы
        [JsonPropertyName("html")] public bool? Html { get; init; }
        [JsonPropertyName("css")] public bool? Css { get; init; }
        [JsonPropertyName("txt")] public bool? Txt { get; init; }
        [JsonPropertyName("txt80")] public bool? Txt80 { get; init; }
    }
}
