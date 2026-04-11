using System.Text.Json.Serialization;

namespace KIT.GasStation.NewCas.Models
{
    public class GetSateNewCas
    {
        /// <summary>
        /// Статус смены, открыта или закрыта
        /// </summary>
        [JsonPropertyName("dayState")]
        public DayStateNewCas State { get; set; }

        /// <summary>
        /// 24 часа закончились или нет
        /// </summary>
        [JsonPropertyName("isShiftExpired")]
        public bool IsShiftExpired { get; set; }

        /// <summary>
        /// номер последней открытой смены
        /// </summary>
        [JsonPropertyName("shiftNumber")]
        public int ShiftNumber { get; set; }

        /// <summary>
        /// последний номер ФД
        /// </summary>
        [JsonPropertyName("documentNumber")]
        public int DocumentNumber { get; set; }

        /// <summary>
        /// дата/время ККМ
        /// </summary>
        [JsonPropertyName("dateTime")]
        public string? DateTime { get; set; }

        /// <summary>
        /// последний номер ФД типа чек
        /// </summary>
        [JsonPropertyName("billNumber")]
        public int BillNumber { get; set; }

        /// <summary>
        /// сумма чеков продаж
        /// </summary>
        [JsonPropertyName("saleSum")]
        public double SaleSum { get; set; }

        /// <summary>
        /// сумма наличных в кассе
        /// </summary>
        [JsonPropertyName("cashSum")]
        public double CashSum { get; set; }

        /// <summary>
        /// сумма безналичных в кассе
        /// </summary>
        [JsonPropertyName("cashlessSum")]
        public double CashlessSum { get; set; }

        /// <summary>
        /// количество чеков продажи
        /// </summary>
        [JsonPropertyName("saleNumber")]
        public int SaleNumber { get; set; }

        /// <summary>
        /// количество чеков возврата
        /// </summary>
        [JsonPropertyName("saleReturnNumber")]
        public int SaleReturnNumber { get; set; }

        /// <summary>
        /// сумма чеков возвратов продаж
        /// </summary>
        [JsonPropertyName("saleReturnSum")]
        public double SaleReturnSum { get; set; }

        /// <summary>
        /// Регистрационный номер ККМ
        /// </summary>
        [JsonPropertyName("registrationNumber")]
        public string? RegistrationNumber { get; set; }

        /// <summary>
        /// номер ФМ
        /// </summary>
        [JsonPropertyName("fmNumber")]
        public string? FmNumber { get; set; }

        /// <summary>
        /// серийный номер ККМ
        /// </summary>
        [JsonPropertyName("serialNumber")]
        public string? SerialNumber { get; set; }
    }
}
