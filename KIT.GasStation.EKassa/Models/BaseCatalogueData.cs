using System.Text.Json.Serialization;

namespace KIT.GasStation.EKassa.Models
{
    public sealed record BaseCatalogueData
    {
        [JsonPropertyName("version")]
        public int Version { get; init; }

        [JsonPropertyName("items")]
        public List<BaseCatalogueItem> Items { get; init; } = new();

        [JsonPropertyName("taxes")]
        public List<BaseCatalogueTax> Taxes { get; init; } = new();
    }
}
