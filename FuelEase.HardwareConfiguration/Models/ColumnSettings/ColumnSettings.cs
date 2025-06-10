using System.ComponentModel;
using System.Xml.Serialization;

namespace FuelEase.HardwareConfigurations.Models
{
    [Serializable]
    [XmlInclude(typeof(LanfengColumnSettings))]
    [XmlInclude(typeof(PKElectronicsColumnSettings))]
    public abstract class ColumnSettings : INotifyPropertyChanged
    {
        /// <summary>
        /// Блокирована ли колонка
        /// </summary>
        [XmlAttribute]
        public bool IsDisabled { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
