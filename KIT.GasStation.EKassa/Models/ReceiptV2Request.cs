using System.Text.Json.Serialization;

namespace KIT.GasStation.EKassa.Models
{
    public sealed record ReceiptV2Request
    {
        [JsonPropertyName("fiscal_number")]
        public string FiscalNumber { get; init; } = default!;

        /// <summary>Если true — в теге txt вернётся чек в текстовом формате на 32 символа в строке.</summary>
        [JsonPropertyName("txt")]
        public bool? Txt { get; init; }

        /// <summary>Если true — в теге txt вернётся чек в текстовом формате на 42 символа в строке.</summary>
        [JsonPropertyName("txt80")]
        public bool? Txt80 { get; init; }

        /// <summary>Если true — вернётся чек в формате html.</summary>
        [JsonPropertyName("html")]
        public bool? Html { get; init; }

        /// <summary>Используется в связке с html:true. Добавляет CSS-стили для печати.</summary>
        [JsonPropertyName("css")]
        public bool? Css { get; init; }

        /// <summary>
        /// Получено (тийины, строкой). Если присутствует — рассчитывается сдача и отражается в чеке.
        /// Пример: "500000". Не передавать если не требуется.
        /// </summary>
        [JsonPropertyName("received")]
        public string? Received { get; init; }

        /// <summary>
        /// Скидка (тийины, строкой). Если присутствует — отражается в чеке.
        /// Пример: "500". Не передавать (null) если скидки нет.
        /// </summary>
        [JsonPropertyName("discount")]
        public string? Discount { get; init; }

        /// <summary>
        /// Наличные (true) / Безналичные (false). Если не указан или true — наличные.
        /// </summary>
        [JsonPropertyName("cash")]
        public bool? Cash { get; init; }

        /// <summary>Адрес электронной почты покупателя. ГНС отправит чек на почту покупателя.</summary>
        [JsonPropertyName("customerContact")]
        public string? CustomerContact { get; init; }

        /// <summary>(Обяз.) Список позиций чека.</summary>
        [JsonPropertyName("goods")]
        public List<ReceiptGood> Goods { get; init; } = new();

        /// <summary>(Обяз.) Тип операции: INCOME, INCOME_RETURN, EXPENDITURE, EXPENDITURE_RETURN.</summary>
        [JsonPropertyName("operation")]
        public ReceiptOperation Operation { get; init; }

        /// <summary>
        /// (Обяз. для INCOME_RETURN / EXPENDITURE_RETURN)
        /// Номер ФД документа-основания, по которому осуществляется возврат (тэг 1040).
        /// </summary>
        [JsonPropertyName("originFdNumber")]
        public int? OriginFdNumber { get; init; }
    }
}
