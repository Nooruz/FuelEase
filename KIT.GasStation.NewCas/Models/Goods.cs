using System.Text.Json.Serialization;

namespace KIT.GasStation.NewCas.Models
{
    /// <summary>
    /// Товары или услуги
    /// </summary>
    public class Goods
    {
        /// <summary>
        /// количество
        /// </summary>
        [JsonPropertyName("count")]
        public decimal Count { get; set; }

        /// <summary>
        /// Цена
        /// </summary>
        [JsonPropertyName("price")]
        public decimal Price { get; set; }

        /// <summary>
        /// Наименование товара или услуги
        /// </summary>
        [JsonPropertyName("itemName")]
        public string ItemName { get; set; }

        /// <summary>
        /// Артикул
        /// </summary>
        [JsonPropertyName("article")]
        public string? Article { get; set; }

        /// <summary>
        /// Сумма
        /// </summary>
        [JsonPropertyName("total")]
        public string Total { get; set; }

        /// <summary>
        /// Единица измерения
        /// </summary>
        [JsonPropertyName("unit")]
        public string Unit { get; set; }

        /// <summary>
        /// код ставки НДС
        /// </summary>
        [JsonPropertyName("vatNum")]
        public int VatNum { get; set; }

        /// <summary>
        /// код ставки НСП
        /// </summary>
        [JsonPropertyName("stNum")]
        public int StNum { get; set; }

        [JsonPropertyName("discounts")]
        public Discounts[]? Discounts { get; set; }
    }
}
