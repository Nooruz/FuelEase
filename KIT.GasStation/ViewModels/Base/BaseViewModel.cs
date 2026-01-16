using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using System;
using System.ComponentModel;

namespace KIT.GasStation.ViewModels.Base
{
    public delegate TViewModel CreateViewModel<TViewModel>() where TViewModel : BaseViewModel;
    [POCOViewModel]
    public class BaseViewModel : ViewModelBase, IDisposable
    {
        #region PropertyChanged

        protected void OnPropertyChanged(string propertyName)
        {
            RaisePropertyChanged(propertyName);
        }

        private string _title;

        #endregion

        #region Private Members

        private bool _disposed;

        #endregion

        #region Public Properties

        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                OnPropertyChanged(nameof(Title));
            }
        }

        #endregion

        #region Constructor

        public BaseViewModel()
        {

        }

        #endregion

        #region Public Voids

        [Command]
        public void Close()
        {
            CurrentWindowService?.Close();
        }

        #endregion

        #region Dialog Services

        protected IWindowService WindowService => GetService<IWindowService>("DialogService");
        protected IWindowService DocumentViewerService => GetService<IWindowService>("DocumentViewerService");
        protected IDialogService DialogService => GetService<IDialogService>();
        protected ICurrentWindowService CurrentWindowService => GetService<ICurrentWindowService>();
        protected IMessageBoxService MessageBoxService => GetService<IMessageBoxService>();
        protected INotificationService NotificationService => GetService<INotificationService>();
        protected INavigationService NavigationService => GetService<INavigationService>();
        protected ISplashScreenManagerService SplashScreenManagerService => GetService<ISplashScreenManagerService>();

        #endregion

        #region Disposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        [Command]
        public void OnWindowClosed()
        {
            Dispose();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // Здесь освобождаем управляемые ресурсы

                // Если подписаны на внешние события — отписываемся!
                // Например: SomeService.SomeEvent -= Handler;
            }

            _disposed = true;
        }

        ~BaseViewModel()
        {
            Dispose(false);
        }

        #endregion
    }
}
