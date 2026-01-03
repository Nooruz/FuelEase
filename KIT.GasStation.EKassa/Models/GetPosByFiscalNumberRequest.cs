using System.Text.Json.Serialization;

namespace KIT.GasStation.EKassa.Models
{
    public sealed record GetPosByFiscalNumberRequest
    {
        [JsonPropertyName("fiscal_number")]
        public string FiscalNumber { get; init; } = default!;
    }
}
