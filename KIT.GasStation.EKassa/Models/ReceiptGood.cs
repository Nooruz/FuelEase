using System.Text.Json.Serialization;

namespace KIT.GasStation.EKassa.Models
{
    public sealed record ReceiptGood
    {
        /// <summary>0 – Товар, 1 – Услуга (по PDF).</summary>
        [JsonPropertyName("calcItemAttributeCode")]
        public int CalcItemAttributeCode { get; init; }

        [JsonPropertyName("name")]
        public string Name { get; init; } = default!;

        /// <summary>Код товара (в PDF называется sgtin).</summary>
        [JsonPropertyName("sgtin")]
        public string Sgtin { get; init; } = default!;

        /// <summary>Цена в копейках.</summary>
        [JsonPropertyName("price")]
        public int Price { get; init; }

        /// <summary>Количество.</summary>
        [JsonPropertyName("quantity")]
        public decimal Quantity { get; init; }

        [JsonPropertyName("unit")]
        public string Unit { get; init; } = default!;

        /// <summary>НСП в процентах.</summary>
        [JsonPropertyName("st")]
        public int St { get; init; }

        /// <summary>НДС в процентах.</summary>
        [JsonPropertyName("vat")]
        public int Vat { get; init; }
    }
}
