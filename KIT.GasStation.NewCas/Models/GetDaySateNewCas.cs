using System.Text.Json.Serialization;

namespace KIT.GasStation.NewCas.Models
{
    public sealed class GetDaySateNewCas
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
        /// дата/время смены
        /// </summary>
        [JsonPropertyName("shiftDateTime")]
        public DateTime ShiftDateTime { get; set; }
    }
}
