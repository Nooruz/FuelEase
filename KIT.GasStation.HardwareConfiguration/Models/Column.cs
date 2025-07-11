using System.Xml.Serialization;

namespace KIT.GasStation.HardwareConfigurations.Models
{
    /// <summary>
    /// Колонка
    /// </summary>
    [Serializable]
    public class Column : DomainObject
    {
        #region Private Members

        private Guid _id = Guid.NewGuid();
        private string _name;
        private int _address;
        private int _nozzle;
        private ColumnSettings _settings;
        private ConnectionStatus _connectionStatus;

        #endregion

        #region Public Properties

        /// <summary>
        /// Идентификатор колонки
        /// </summary>
        [XmlAttribute]
        public Guid Id
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged(nameof(Id));
            }
        }

        /// <summary>
        /// Имя колонки
        /// </summary>
        [XmlAttribute]
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        /// <summary>
        /// Адрес колонки
        /// </summary>
        [XmlAttribute]
        public int Address
        {
            get => _address;
            set
            {
                _address = value;
                OnPropertyChanged(nameof(Address));
            }
        }

        /// <summary>
        /// Номер пистолета
        /// </summary>
        [XmlAttribute]
        public int Nozzle
        {
            get => _nozzle;
            set
            {
                _nozzle = value;
                OnPropertyChanged(nameof(Nozzle));
            }
        }

        /// <summary>
        /// Статус подключения
        /// </summary>
        [XmlIgnore]
        public ConnectionStatus ConnectionStatus
        {
            get => _connectionStatus;
            set
            {
                _connectionStatus = value;
                OnPropertyChanged(nameof(ConnectionStatus));
            }
        }

        /// <summary>
        /// Настройки колонки
        /// </summary>
        public ColumnSettings Settings
        {
            get => _settings;
            set
            {
                _settings = value;
                OnPropertyChanged(nameof(Settings));
            }
        }

        [XmlIgnore]
        public Controller Controller { get; set; }

        [XmlIgnore]
        public string DisplayName => $"{Controller?.Name} / {Name}";

        #endregion

        #region Public Methods

        public override void Update(DomainObject updatedItem)
        {
            if (updatedItem is Column column)
            {
               
            }
        }

        #endregion
    }

    public enum ConnectionStatus
    {
        /// <summary>
        /// Не проверено
        /// </summary>
        NotVerified,

        /// <summary>
        /// Проверяется
        /// </summary>
        BeingVerified,

        /// <summary>
        /// Не подключен
        /// </summary>
        NotConnected,

        /// <summary>
        /// Подключен
        /// </summary>
        Connected
    }
}
