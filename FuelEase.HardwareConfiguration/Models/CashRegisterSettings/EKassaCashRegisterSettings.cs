using System.Xml.Serialization;

namespace FuelEase.HardwareConfigurations.Models
{
    [Serializable]
    public class EKassaCashRegisterSettings : CashRegisterSettings
    {
        #region Private Members

        private string? _defaultPrinterName = "Не задан";
        private TapeType _tapeType = TapeType.TXT80;

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

        /// <summary>
        /// Тип ленты
        /// </summary>
        [XmlAttribute]
        public TapeType TapeType
        {
            get => _tapeType;
            set
            {
                _tapeType = value;
                OnPropertyChanged(nameof(TapeType));
            }
        }

        #endregion
    }

    /// <summary>
    /// Тип ленты
    /// </summary>
    public enum TapeType
    {
        TXT,

        TXT80
    }
}
