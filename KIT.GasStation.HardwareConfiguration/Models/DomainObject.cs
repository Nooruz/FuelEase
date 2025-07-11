using System.ComponentModel;

namespace KIT.GasStation.HardwareConfigurations.Models
{
    public abstract class DomainObject : INotifyPropertyChanged
    {
        #region PropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Abstracts

        public abstract void Update(DomainObject updatedItem);

        #endregion
    }
}
