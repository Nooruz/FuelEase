using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace KIT.GasStation.HardwareConfigurations.Models
{
    /// <summary>
    /// Конфигурация оборудования
    /// </summary>
    [Serializable]
    [XmlRoot("HardwareConfiguration")]
    public class HardwareConfiguration : DomainObject
    {
        #region Private Members

        private ObservableCollection<Controller> _controllers = new();
        private ObservableCollection<CashRegister> _cashRegisters = new();

        #endregion

        #region Public Properties

        [XmlArray("Controllers")]
        [XmlArrayItem("Controller")]
        public ObservableCollection<Controller> Controllers
        {
            get => _controllers;
            set
            {
                _controllers = value;
                OnPropertyChanged(nameof(Controllers));
            }
        }

        [XmlArray("CashRegisters")]
        [XmlArrayItem("CashRegister")]
        public ObservableCollection<CashRegister> CashRegisters
        {
            get => _cashRegisters;
            set
            {
                _cashRegisters = value;
                OnPropertyChanged(nameof(CashRegisters));
            }
        }

        #endregion

        #region Public Methods

        public override void Update(DomainObject updatedItem)
        {

        }

        #endregion
    }
}
