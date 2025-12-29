using System.ComponentModel.DataAnnotations;

namespace KIT.GasStation.NewCas.Models
{
    public enum DayStateNewCas
    {
        /// <summary>
        /// Смена закрыта
        /// </summary>
        [Display(Name = "Закрыта")]
        ShiftClosed = 0,

        /// <summary>
        /// Смена открыта
        /// </summary>
        [Display(Name = "Открыта")]
        ShiftOpened = 1,

        None = 2
    }
}
