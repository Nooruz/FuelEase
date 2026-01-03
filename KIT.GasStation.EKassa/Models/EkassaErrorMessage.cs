using System.Text.Json.Serialization;

namespace KIT.GasStation.EKassa.Models
{
    /// <summary>
    /// Удобная модель для ошибки (когда message = объект).
    /// </summary>
    public sealed record EkassaErrorMessage
    {
        [JsonPropertyName("code")]
        public string? Code { get; init; }

        [JsonPropertyName("error")]
        public string? Error { get; init; }
    }
}
