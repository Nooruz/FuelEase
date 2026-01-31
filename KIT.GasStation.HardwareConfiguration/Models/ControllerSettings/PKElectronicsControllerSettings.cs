using System.ComponentModel.DataAnnotations;

namespace KIT.GasStation.HardwareConfigurations.Models
{
    /// <summary>
    /// Настройка ТРК ПК Электроникс
    /// </summary>
    [Serializable]
    public class PKElectronicsControllerSettings : ControllerSettings
    {
        /// <summary>
        /// Метод опроса
        /// </summary>
        public PollingMode PollingMode { get; set; }

        /// <summary>
        /// Количество пистолетов на стороне
        /// </summary>
        public NozzlesPerSide NozzlesPerSide { get; set; }

        #region Public Voids

        public override void SetStatus(object status)
        {

        }

        public override object GetStatus()
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    /// <summary>
    /// Метод опроса
    /// </summary>
    public enum PollingMode
    {
        /// <summary>
        /// Пистолеты
        /// </summary>
        [Display(Name = "Пистолеты")]
        Nozzle,

        /// <summary>
        /// Стороны
        /// </summary>
        [Display(Name = "Стороны")]
        Side
    }

    /// <summary>
    /// Количество пистолетов на стороне
    /// </summary>
    public enum NozzlesPerSide
    {
        /// <summary>
        /// 4 пистолета
        /// </summary>
        [Display(Name = "4 пистолета")]
        Four,

        /// <summary>
        /// 5 пистолетов
        /// </summary>
        [Display(Name = "5 пистолетов")]
        Five
    }
}
