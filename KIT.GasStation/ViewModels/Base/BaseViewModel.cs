using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

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
        private readonly StringBuilder _buffer = new();
        private CancellationTokenSource? _bufferCts;
        private const int MaxDigits = 2;
        private const int TimeoutMs = 1200; // подстрой: 800–1500 обычно норм (1.2 секунды)

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

        public string BufferText => _buffer.ToString();

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

        #region Public Virtual Voids

        protected virtual bool HandleHotKey(Key key) => true;

        #endregion

        #region Hot Keys Command

        [Command]
        public void PreviewKeyDown(object args)
        {
            if (args is not KeyEventArgs e) return;

            // Сначала пробуем хоткеи/переопределение
            if (HandleHotKey(e.Key))
            {
                e.Handled = true;
                return;
            }

            // Цифры (верхний ряд и NumPad)
            if (TryGetDigit(e.Key, out var digit))
            {
                e.Handled = true; // <-- "команда должна остановиться": дальше по дереву не пойдёт

                // если уже набрано 2 цифры — начинаем заново с этой цифры
                if (_buffer.Length >= MaxDigits)
                    _buffer.Clear();

                _buffer.Append(digit);
                RaisePropertyChanged(nameof(BufferText));

                RestartTimeout();

                // если уже 2 цифры — считаем что ввод завершён
                if (_buffer.Length == MaxDigits)
                    //CommitColumnSelection();

                return;
            }



        }

        #endregion

        #region Helper

        private static bool TryGetDigit(Key key, out char digit)
        {
            digit = default;

            // Верхний ряд цифр
            if (key >= Key.D0 && key <= Key.D9)
            {
                digit = (char)('0' + (key - Key.D0));
                return true;
            }

            // NumPad
            if (key >= Key.NumPad0 && key <= Key.NumPad9)
            {
                digit = (char)('0' + (key - Key.NumPad0));
                return true;
            }

            return false;
        }

        private void RestartTimeout()
        {
            _bufferCts?.Cancel();
            _bufferCts?.Dispose();
            _bufferCts = new CancellationTokenSource();

            var token = _bufferCts.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(TimeoutMs, token);

                    // вернуться в UI-поток
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        ClearBuffer();
                    });
                }
                catch (TaskCanceledException) { /* норм */ }
            }, token);
        }

        private void ClearBuffer()
        {
            _buffer.Clear();
            RaisePropertyChanged(nameof(BufferText));
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
