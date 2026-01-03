using System.Text.Json.Serialization;

namespace KIT.GasStation.EKassa.Models
{
    public sealed record CashOperationData
    {
        [JsonPropertyName("id")]
        public string Id { get; init; } = default!;

        [JsonPropertyName("kassa_cash_prihod")]
        public string? KassaCashPrihod { get; init; }

        [JsonPropertyName("kassa_shift_cash_prihod")]
        public string? KassaShiftCashPrihod { get; init; }

        [JsonPropertyName("kassa_total_cash_prihod")]
        public string? KassaTotalCashPrihod { get; init; }

        [JsonPropertyName("total_kassa_cash")]
        public string? TotalKassaCash { get; init; }

        [JsonPropertyName("html")]
        public string? Html { get; init; }

        [JsonPropertyName("txt")]
        public string? Txt { get; init; }
    }
}
