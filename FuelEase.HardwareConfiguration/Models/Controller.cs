using FuelEase.HardwareConfigurations.Services;
using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace FuelEase.HardwareConfigurations.Models
{
    /// <summary>
    /// Контроллер
    /// </summary>
    [Serializable]
    public class Controller : DomainObject, IDevice<ControllerType>
    {
        #region Private Members

        private Guid _id = Guid.NewGuid();
        private string _name;
        private ControllerType _type;
        private string _comPort;
        private int _baudRate = 4800;
        private ControllerSettings _settings;
        private ObservableCollection<Column> _columns = new();

        #endregion

        #region Public Properties

        /// <summary>
        /// Идентификатор контроллера
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
        /// Имя контроллера
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
        /// Тип контроллера
        /// </summary>
        [XmlAttribute]
        public ControllerType Type
        {
            get => _type;
            set
            {
                _type = value;
                OnPropertyChanged(nameof(Type));
            }
        }

        /// <summary>
        /// Порт
        /// </summary>
        [XmlAttribute]
        public string ComPort
        {
            get => _comPort;
            set
            {
                _comPort = value;
                OnPropertyChanged(nameof(ComPort));
            }
        }

        /// <summary>
        /// Скорость обмена
        /// </summary>
        [XmlAttribute]
        public int BaudRate
        {
            get => _baudRate;
            set
            {
                _baudRate = value;
                OnPropertyChanged(nameof(BaudRate));
            }
        }

        /// <summary>
        /// Настройки контроллера
        /// </summary>
        public ControllerSettings Settings
        {
            get => _settings;
            set
            {
                _settings = value;
                OnPropertyChanged(nameof(Settings));
            }
        }

        /// <summary>
        /// Коллекция колонок, связанных с контроллером.
        /// </summary>
        [XmlArray("Columns")]
        [XmlArrayItem("Column")]
        public ObservableCollection<Column> Columns
        {
            get => _columns;
            set
            {
                _columns = value;
                OnPropertyChanged(nameof(Columns));
            }
        }

        #endregion

        #region Public Methods

        public override void Update(DomainObject updatedItem)
        {
            if (updatedItem is Controller controller)
            {
                Name = controller.Name;
                Type = controller.Type;
                ComPort = controller.ComPort;
                BaudRate = controller.BaudRate;
                Columns = controller.Columns;
            }
        }

        #endregion
    }

}
