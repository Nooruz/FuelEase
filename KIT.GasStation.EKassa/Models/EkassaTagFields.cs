using System.Text.Json;
using System.Text.Json.Serialization;

namespace KIT.GasStation.EKassa.Models
{
    /// <summary>
    /// Динамические поля "fields" (теги ФФД), содержат вложенные объекты/массивы.
    /// </summary>
    public sealed record EkassaTagFields
    {
        [JsonExtensionData]
        public Dictionary<string, JsonElement> Tags { get; init; } = new();
    }
}
