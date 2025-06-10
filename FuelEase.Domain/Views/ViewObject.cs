using System.ComponentModel;

namespace FuelEase.Domain.Views
{
    public class ViewObject : INotifyPropertyChanged
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
