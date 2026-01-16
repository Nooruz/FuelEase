using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Xpf.Core;
using KIT.GasStation.Domain.Models;
using KIT.GasStation.Services;
using KIT.GasStation.SplashScreen;
using KIT.GasStation.State.Navigators;
using KIT.GasStation.State.Users;
using KIT.GasStation.ViewModels.Base;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace KIT.GasStation.ViewModels
{
    public class MainWindowViewModel : BaseViewModel, IHostedService
    {
        #region Private Members

        private readonly ILogger<MainWindowViewModel> _logger;
        private readonly INavigator _navigator;
        private readonly ICustomSplashScreenService _splashScreenService;
        private readonly IUserStore _userStore;
        private readonly IHotKeysService _hotKeysService;
        private readonly StringBuilder _buffer = new();
        private CancellationTokenSource? _bufferCts;
        private const int MaxDigits = 2;
        private const int TimeoutMs = 1200; // подстрой: 800–1500 обычно норм (1.2 секунды)

        #endregion

        #region Public Properties

        public BaseViewModel CurrentViewModel => _navigator.CurrentViewModel;
        public string UserName
        {
            get
            {
                if (_userStore != null)
                {
                    if (_userStore.CurrentUser != null)
                    {
                        return $"{App.ProductName} Работает: {_userStore.CurrentUser.FullName}";
                    }
                }
                return "";
            }
        }
        public string BufferText => _buffer.ToString();

        #endregion

        #region Constructor

        public MainWindowViewModel(INavigator navigator,
            ILogger<MainWindowViewModel> logger,
            ICustomSplashScreenService splashScreenService,
            IUserStore userStore,
            IHotKeysService hotKeysService)
        {
            _logger = logger;
            _navigator = navigator;
            _splashScreenService = splashScreenService;
            _userStore = userStore;
            _hotKeysService = hotKeysService;

            _navigator.StateChanged += Navigator_StateChanged;

            _splashScreenService.OnShow += SplashScreenService_OnShow;
            _splashScreenService.OnClose += SplashScreenService_OnClose;

            _userStore.OnLogin += UserStore_OnLogin;
        }

        #endregion

        #region Public Voids

        [Command]
        public void Show()
        {
            if (SplashScreenManagerService.State == SplashScreenState.Shown)
            {
                SplashScreenManagerService.Close();
            }
            else
            {
                SplashScreenManagerService.Show();
            }
        }

        [Command]
        public void Closing(CancelEventArgs args)
        {
            if (MessageBoxService == null)
            {
                return;
            }

            var result = MessageBoxService.ShowMessage("Вы уверены, что хотите закрыть программу?", "Подтверждение", MessageButton.YesNo, MessageIcon.Question);

            if (result == MessageResult.No)
            {
                args.Cancel = true;
            }
        }

        [Command]
        public void MainWindowLoaded(RoutedEventArgs args)
        {
            if (args.Source is ThemedWindow window)
            {
                window.IsVisibleChanged += (s, e) =>
                {
                    if ((bool)e.NewValue)
                    {
                        OnPropertyChanged(nameof(UserName));
                    }
                };
            }
        }

        #endregion

        #region Hot Keys

        [Command]
        public void PreviewKeyDown(object args)
        {
            if (args is not KeyEventArgs e) return;

            // Цифры (верхний ряд и NumPad)
            if (TryGetDigit(e.Key, out var digit))
            {
                e.Handled = true; // <-- "команда должна остановиться": дальше по дереву не пойдёт

                // если уже набрано 2 цифры — начинаем заново с этой цифры
                if (_buffer.Length >= MaxDigits)
                    _buffer.Clear();

                _buffer.Append(digit);
                RaisePropertyChanged(nameof(BufferText));

                // если уже 2 цифры — считаем что ввод завершён
                if (int.TryParse(_buffer.ToString(), out var number))
                    _hotKeysService.HandleNumberKeyPress(number);

                return;
            }
            _hotKeysService.HandleKeyPress(e.Key);
        }

        #endregion

        #region Helpers

        private static bool TryGetDigit(Key key, out char digit)
        {
            digit = default;

            // Верхний ряд цифр
            if (key >= Key.D0 && key <= Key.D9)
            {
                digit = (char)('0' + (key - Key.D0));
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
                        CommitBuffer();
                    });
                }
                catch (TaskCanceledException) { /* норм */ }
            }, token);
        }

        private void CommitBuffer()
        {
            if (_buffer.Length == 0) return;

            if (int.TryParse(_buffer.ToString(), out var number))
                _hotKeysService.HandleNumberKeyPress(number);

            _buffer.Clear();
            RaisePropertyChanged(nameof(BufferText));
        }

        #endregion

        #region Private Voids

        private void Navigator_StateChanged()
        {
            OnPropertyChanged(nameof(CurrentViewModel));
        }

        private void UserStore_OnLogin(User user)
        {
            OnPropertyChanged(nameof(UserName));
        }

        #endregion

        #region Splash Screen

        private void SplashScreenService_OnShow(DXSplashScreenViewModel viewModel)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    SplashScreenManagerService.ViewModel = viewModel;
                    SplashScreenManagerService.Show();
                    _splashScreenService.CustomSplashScreenState = CustomSplashScreenState.Showing;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, e.Message);
                    _splashScreenService.CustomSplashScreenState = CustomSplashScreenState.Closing;
                }
            });
        }

        private void SplashScreenService_OnClose()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    SplashScreenManagerService.Close();
                    _splashScreenService.CustomSplashScreenState = CustomSplashScreenState.Closing;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, e.Message);
                    _splashScreenService.CustomSplashScreenState = CustomSplashScreenState.Closing;
                }
            });
        }

        #endregion

        #region HostedService

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _navigator.CurrentViewModel = await _navigator.GetViewModelAsync(ViewType.Main);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            
        }

        #endregion
    }
}
