using System.Text.Json;
using System.Text.Json.Serialization;

namespace KIT.GasStation.EKassa.Models
{
    /// <summary>
    /// Общая обёртка ответа eKassa:
    /// { "status": "Success|Error", "message": ..., "data": ... }
    /// </summary>
    public sealed record EkassaResponse<TData>
    {
        [JsonPropertyName("status")]
        public string Status { get; init; } = default!;

        /// <summary>
        /// В успешном ответе обычно строка.
        /// В ошибке может быть объект вида { code: "...", error: "..." }.
        /// Поэтому JsonElement.
        /// </summary>
        [JsonPropertyName("message")]
        public JsonElement Message { get; init; }

        [JsonPropertyName("data")]
        public TData? Data { get; init; }
    }
}
