using System.ComponentModel.DataAnnotations;

namespace KIT.GasStation.Domain.Models
{
    /// <summary>
    /// Счётчик порядковых номеров документов.
    /// Ключ — составной: (DocumentType, PeriodKey).
    /// Например: ("FuelSale", "2026-04-04") или ("Shift", "2026").
    /// </summary>
    public class DocumentCounter
    {
        /// <summary>
        /// Тип документа: "FuelSale", "Shift" и т.д.
        /// </summary>
        [MaxLength(50)]
        public string DocumentType { get; set; }

        /// <summary>
        /// Ключ периода: дата "yyyy-MM-dd" для FuelSale, год "yyyy" для Shift.
        /// </summary>
        [MaxLength(20)]
        public string PeriodKey { get; set; }

        /// <summary>
        /// Текущее значение счётчика (последний выданный номер).
        /// </summary>
        public int CurrentValue { get; set; }
    }
}
