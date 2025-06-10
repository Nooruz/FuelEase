using System.ComponentModel;
using System.Xml.Serialization;

namespace FuelEase.HardwareConfigurations.Models
{
    [Serializable]
    [XmlInclude(typeof(EKassaCashRegisterSettings))]
    public abstract class CashRegisterSettings : INotifyPropertyChanged
    {
        #region PropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
