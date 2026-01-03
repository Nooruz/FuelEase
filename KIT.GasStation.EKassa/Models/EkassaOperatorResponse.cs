using System.Text.Json;
using System.Text.Json.Serialization;

namespace KIT.GasStation.EKassa.Models
{
    /// <summary>
    /// Часто встречается operatorResponse: { "1040": "...", "1041": "...", "1012": "...", "1206": "...", "1017": "..." }
    /// </summary>
    public sealed record EkassaOperatorResponse
    {
        [JsonExtensionData]
        public Dictionary<string, JsonElement> Values { get; init; } = new();
    }
}
