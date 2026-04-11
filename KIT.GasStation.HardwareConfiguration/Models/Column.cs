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
        private bool _isLifted;
        private decimal _systemCounter;

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
        /// Системный счетчик
        /// </summary>
        [XmlAttribute]
        public decimal SystemCounter
        {
            get => _systemCounter;
            set
            {
                _systemCounter = value;
                OnPropertyChanged(nameof(SystemCounter));
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

        [XmlIgnore]
        public decimal Price { get; set; }

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
        public string GroupName => Controller != null ? $"{Controller.Name}/{Name}" : string.Empty;

        /// <summary>
        /// Адрес пистолета для протокола Lanfeng в виде степени двойки.
        /// </summary>
        [XmlIgnore]
        public int LanfengAddress => 1 << (Nozzle - 1);

        /// <summary>
        /// Признак поднятого пистолета
        /// </summary>
        [XmlIgnore]
        public bool IsLifted
        {
            get => _isLifted;
            set
            {
                _isLifted = value;
                OnPropertyChanged(nameof(IsLifted));
            }
        }

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
