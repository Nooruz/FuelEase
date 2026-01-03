using System.Text.Json.Serialization;

namespace KIT.GasStation.EKassa.Models
{
    /// <summary>
    /// Универсальная модель данных ответа на shift_open/shift_close/shift_state.
    /// </summary>
    public sealed record ShiftReportData
    {
        [JsonPropertyName("id")]
        public string Id { get; init; } = default!;

        [JsonPropertyName("fiscalNumber")]
        public string FiscalNumber { get; init; } = default!;

        [JsonPropertyName("fields")]
        public EkassaTagFields? Fields { get; init; }

        [JsonPropertyName("html")]
        public string? Html { get; init; }

        [JsonPropertyName("txt")]
        public string? Txt { get; init; }

        [JsonPropertyName("operatorResponse")]
        public EkassaOperatorResponse? OperatorResponse { get; init; }
    }
}
