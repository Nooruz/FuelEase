using System.ComponentModel;
using System.Xml.Serialization;

namespace KIT.GasStation.HardwareConfigurations.Models
{
    [Serializable]
    [XmlInclude(typeof(EKassaCashRegisterSettings))]
    [XmlInclude(typeof(NewCasCashRegisterSettings))]
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
