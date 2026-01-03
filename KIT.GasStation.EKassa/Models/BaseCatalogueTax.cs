using System.Text.Json.Serialization;

namespace KIT.GasStation.EKassa.Models
{
    public sealed record BaseCatalogueTax
    {
        [JsonPropertyName("type")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public TaxType Type { get; init; }

        [JsonPropertyName("code")]
        public int Code { get; init; }

        [JsonPropertyName("value")]
        public decimal Value { get; init; }
    }
}
