using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using KIT.GasStation.SplashScreen;
using KIT.GasStation.State.Navigators;
using KIT.GasStation.ViewModels.Base;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace KIT.GasStation.ViewModels
{
    public class MainWindowViewModel : BaseViewModel, IHostedService
    {
        #region Private Members

        private readonly ILogger<MainWindowViewModel> _logger;
        private readonly INavigator _navigator;
        private readonly ICustomSplashScreenService _splashScreenService;

        #endregion

        #region Public Properties

        public BaseViewModel CurrentViewModel => _navigator.CurrentViewModel;
        
        #endregion

        #region Constructor

        public MainWindowViewModel(INavigator navigator,
            ILogger<MainWindowViewModel> logger,
            ICustomSplashScreenService splashScreenService)
        {
            _logger = logger;
            _navigator = navigator;
            _splashScreenService = splashScreenService;

            _navigator.StateChanged += Navigator_StateChanged;

            _splashScreenService.OnShow += SplashScreenService_OnShow;
            _splashScreenService.OnClose += SplashScreenService_OnClose;
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

        #endregion

        #region Private Voids

        private void Navigator_StateChanged()
        {
            OnPropertyChanged(nameof(CurrentViewModel));
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
