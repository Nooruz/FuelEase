using System.Xml.Serialization;

namespace KIT.GasStation.HardwareConfigurations.Models
{
    [Serializable]
    public class NewCasCashRegisterSettings : CashRegisterSettings
    {
        #region Private Members

        private string? _defaultPrinterName = "Не задан";

        #endregion

        #region Public Properties

        /// <summary>
        /// Принтер по умолчанию
        /// </summary>
        [XmlAttribute]
        public string? DefaultPrinterName
        {
            get => _defaultPrinterName;
            set
            {
                _defaultPrinterName = value;
                OnPropertyChanged(nameof(DefaultPrinterName));
            }
        }

        #endregion
    }
}
