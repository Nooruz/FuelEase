using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using System.ComponentModel;

namespace FuelEase.Hardware.ViewModels
{
    public delegate TViewModel CreateViewModel<TViewModel>() where TViewModel : BaseViewModel;
    [POCOViewModel]
    public class BaseViewModel : ViewModelBase, INotifyPropertyChanged
    {
        #region Dialog Services

        protected IWindowService WindowService => GetService<IWindowService>("DialogService");
        protected ICurrentWindowService CurrentWindowService => GetService<ICurrentWindowService>();
        protected IMessageBoxService MessageBoxService => GetService<IMessageBoxService>();
        protected ISplashScreenManagerService SplashScreenManagerService => GetService<ISplashScreenManagerService>();
        protected IDialogService DialogService => GetService<IDialogService>();

        #endregion

        #region PropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Dispose

        public virtual void Dispose()
        {

        }

        #endregion
    }
}
