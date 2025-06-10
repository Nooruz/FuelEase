using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace FuelEase.HardwareConfigurations.Models
{
    /// <summary>
    /// Настройки пистолетов ПК электроникс 
    /// </summary>
    [Serializable]
    public class PKElectronicsColumnSettings : ColumnSettings
    {
        #region Private Members

        private ColumnSensorType _columnSensorType = ColumnSensorType.NormallyClosed;
        private int _pumpAccelerationTime = 2000;
        private int _reduceCosts = 25;
        private int _transfuse = 160;
        private bool _blockingChannelOperation;

        #endregion

        #region Public Properties

        /// <summary>
        /// Датчик пистолета
        /// </summary>
        [XmlAttribute]
        public ColumnSensorType ColumnSensorType
        {
            get => _columnSensorType;
            set
            {
                _columnSensorType = value;
                OnPropertyChanged(nameof(ColumnSensorType));
            }
        }

        /// <summary>
        /// Время разгона насоса
        /// </summary>
        [XmlAttribute]
        public int PumpAccelerationTime
        {
            get => _pumpAccelerationTime;
            set
            {
                _pumpAccelerationTime = value;
                OnPropertyChanged(nameof(PumpAccelerationTime));
            }
        }

        /// <summary>
        /// Константа снижения расхода
        /// </summary>
        [XmlAttribute]
        public int ReduceCosts
        {
            get => _reduceCosts;
            set
            {
                _reduceCosts = value;
                OnPropertyChanged(nameof(ReduceCosts));
            }
        }

        /// <summary>
        /// Константа перелива
        /// </summary>
        [XmlAttribute]
        public int Transfuse
        {
            get => _transfuse;
            set
            {
                _transfuse = value;
                OnPropertyChanged(nameof(Transfuse));
            }
        }

        /// <summary>
        /// Блокировка работы канала
        /// </summary>
        [XmlAttribute]
        public bool BlockingChannelOperation
        {
            get => _blockingChannelOperation;
            set
            {
                _blockingChannelOperation = value;
                OnPropertyChanged(nameof(BlockingChannelOperation));
            }
        }

        #endregion
    }

    /// <summary>
    /// Датчик пистолета
    /// </summary>
    public enum ColumnSensorType
    {
        /// <summary>
        /// Нормально разомкнутый
        /// </summary>
        [Display(Name = "Нормальноразомкнутый")]
        NormallyOpened,

        /// <summary>
        /// Нормально замкнутый
        /// </summary>
        [Display(Name = "Нормальнозамкнутый")]
        NormallyClosed,

        /// <summary>
        /// Не передавать
        /// </summary>
        [Display(Name = "Не передават")]
        None
    }
}
