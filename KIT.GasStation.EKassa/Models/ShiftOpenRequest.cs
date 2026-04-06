using System.Text.Json.Serialization;

namespace KIT.GasStation.EKassa.Models
{
    public sealed record ShiftOpenRequest
    {
        /// <summary>(Обяз.) Регистрационный налоговый номер ККМ.</summary>
        [JsonPropertyName("fiscal_number")]
        public string FiscalNumber { get; init; } = default!;

        /// <summary>Если true — вернётся чек в формате html.</summary>
        [JsonPropertyName("html")] public bool? Html { get; init; }

        /// <summary>Используется в связке с html:true. Добавляет CSS-стили для печати.</summary>
        [JsonPropertyName("css")] public bool? Css { get; init; }

        /// <summary>Если true — вернётся чек в текстовом формате на 32 символа в строке.</summary>
        [JsonPropertyName("txt")] public bool? Txt { get; init; }

        /// <summary>Если true — вернётся чек в текстовом формате на 42 символа в строке.</summary>
        [JsonPropertyName("txt80")] public bool? Txt80 { get; init; }
    }
}
