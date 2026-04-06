using System.Text.Json.Serialization;

namespace KIT.GasStation.EKassa.Models
{
    public sealed record ReceiptGood
    {
        /// <summary>
        /// (Обяз.) Код предмета расчёта. 0 – Товар, 1 – Услуга.
        /// Полный справочник: /api/info/calc-item-attributes
        /// </summary>
        [JsonPropertyName("calcItemAttributeCode")]
        public int CalcItemAttributeCode { get; init; }

        /// <summary>(Обяз.) Наименование товара/услуги.</summary>
        [JsonPropertyName("name")]
        public string Name { get; init; } = default!;

        /// <summary>(Необяз.) Код товара (ТНВЭД, штрихкод EAN и др.). Сервер eKassa требует наличие поля, даже если пустое.</summary>
        [JsonPropertyName("sgtin")]
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public string? Sgtin { get; init; }

        /// <summary>(Обяз.) Цена товара в тийинах (копейках).</summary>
        [JsonPropertyName("price")]
        public int Price { get; init; }

        /// <summary>(Обяз.) Количество товара.</summary>
        [JsonPropertyName("quantity")]
        public decimal Quantity { get; init; }

        /// <summary>(Обяз.) Единица измерения (например, "шт.", "л.").</summary>
        [JsonPropertyName("unit")]
        public string Unit { get; init; } = default!;

        /// <summary>(Обяз.) Ставка налога НСП в процентах (например, 0, 1, 2, 3, 5).</summary>
        [JsonPropertyName("st")]
        public int St { get; init; }

        /// <summary>(Обяз.) Ставка налога НДС в процентах (0 или 12).</summary>
        [JsonPropertyName("vat")]
        public int Vat { get; init; }
    }
}
