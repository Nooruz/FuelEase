using System.ComponentModel;
using System.Xml.Serialization;

namespace KIT.GasStation.HardwareConfigurations.Models
{
    /// <summary>
    /// Параметры пистолета
    /// </summary>
    [Serializable]
    [XmlInclude(typeof(LanfengColumnSettings))]
    [XmlInclude(typeof(PKElectronicsColumnSettings))]
    [XmlInclude(typeof(GilbarcoColumnSettings))]
    public abstract class ColumnSettings : INotifyPropertyChanged
    {
        #region Private Members

        private ColumnStatus _status;

        #endregion
        
        #region Public Properties

        /// <summary>
        /// Блокирована ли колонка
        /// </summary>
        [XmlAttribute]
        public bool IsDisabled { get; set; }

        /// <summary>
        /// Статус пистолета
        /// </summary>
        public ColumnStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        #endregion

        #region Property Changed

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    /// <summary>
    /// Статус пистолета
    /// </summary>
    public enum ColumnStatus
    {
        /// <summary>
        /// Не выбранный
        /// </summary>
        Unknown,

        /// <summary>
        /// Ожидание
        /// </summary>
        Waiting,

        /// <summary>
        /// Начата заправка
        /// </summary>
        Started,

        /// <summary>
        /// Остановка насоса
        /// </summary>
        PumpStop
    }
}
