using System.Text.Json.Serialization;

namespace KIT.GasStation.EKassa.Models
{
    public sealed record ReceiptV2Request
    {
        [JsonPropertyName("fiscal_number")]
        public string FiscalNumber { get; init; } = default!;

        /// <summary>Если true — вернётся txt (32 символа в строке).</summary>
        [JsonPropertyName("txt")]
        public bool? Txt { get; init; }

        /// <summary>Если true — вернётся txt80 (42 символа в строке).</summary>
        [JsonPropertyName("txt80")]
        public bool? Txt80 { get; init; }

        [JsonPropertyName("html")]
        public bool? Html { get; init; }

        [JsonPropertyName("css")]
        public bool? Css { get; init; }

        /// <summary>Получено (копейки), строкой по примеру из PDF.</summary>
        [JsonPropertyName("received")]
        public string? Received { get; init; }

        /// <summary>Скидка (копейки), строкой по примеру из PDF.</summary>
        [JsonPropertyName("discount")]
        public string? Discount { get; init; }

        /// <summary>
        /// Наличные / Безналичные: если не указан или true – наличные.
        /// </summary>
        [JsonPropertyName("cash")]
        public bool? Cash { get; init; }

        [JsonPropertyName("customerContact")]
        public string? CustomerContact { get; init; }

        [JsonPropertyName("goods")]
        public List<ReceiptGood> Goods { get; init; } = new();

        [JsonPropertyName("operation")]
        public ReceiptOperation Operation { get; init; }

        /// <summary>
        /// Обязателен для возвратов (INCOME_RETURN / EXPENDITURE_RETURN).
        /// Номер ФД документа-основания (тэг 1040).
        /// </summary>
        [JsonPropertyName("originFdNumber")]
        public int? OriginFdNumber { get; init; }
    }
}
