using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Xpf.Docking;
using KIT.GasStation.CashRegisters.Exceptions;
using KIT.GasStation.Domain.Models;
using KIT.GasStation.Domain.Services;
using KIT.GasStation.HardwareConfigurations.Models;
using KIT.GasStation.SplashScreen;
using KIT.GasStation.State.Authenticators;
using KIT.GasStation.State.CashRegisters;
using KIT.GasStation.State.Navigators;
using KIT.GasStation.State.Notifications;
using KIT.GasStation.State.Shifts;
using KIT.GasStation.State.Users;
using KIT.GasStation.ViewModels.Base;
using KIT.GasStation.ViewModels.Factories;
using KIT.GasStation.Views;
using KIT.GasStation.Views.Details;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace KIT.GasStation.ViewModels
{
    public class MainViewModel : BaseViewModel, IAsyncInitializable
    {
        #region Private Members

        private readonly INavigator _navigator;
        private readonly ILogger<MainViewModel> _logger;
        private readonly IShiftStore _shiftStore;
        private readonly IUserStore _userStore;
        private readonly IFuelSaleService _fuelSaleService;
        private readonly IUnregisteredSaleService _unregisteredSaleService;
        private readonly IAuthenticator _authenticator;
        private readonly ICustomSplashScreenService _splashScreenService;
        private readonly ICashRegisterStore _cashRegisterStore;
        private readonly INotificationStore _notificationStore;
        private BaseViewModel _currentViewModel;
        private BaseViewModel _fuelSaleViewModel;
        private BaseViewModel _eventPanelViewModel;
        private BaseViewModel _tanksPanelViewModel;
        private BaseViewModel _cashViewModel;
        private BaseViewModel _nozzleCounterPanelViewModel;
        private BaseViewModel _unregisteredSalePanelViewModel;
        private readonly Dictionary<ViewType, WeakReference<BaseViewModel>> _documentViewModels = new();

        #endregion

        #region Public Properties

        /// <summary>
        /// Текущая ViewModel
        /// </summary>
        public BaseViewModel CurrentViewModel
        {
            get => _currentViewModel;
            private set
            {
                _currentViewModel = value;
                OnPropertyChanged(nameof(CurrentViewModel));
            }
        }

        /// <summary>
        /// ViewModel для продажи топлива
        /// </summary>
        public BaseViewModel FuelSaleViewModel
        {
            get => _fuelSaleViewModel;
            private set
            {
                _fuelSaleViewModel = value;
                OnPropertyChanged(nameof(FuelSaleViewModel));
            }
        }

        /// <summary>
        /// ViewModel для панели событий
        /// </summary>
        public BaseViewModel EventPanelViewModel
        {
            get => _eventPanelViewModel;
            private set
            {
                _eventPanelViewModel = value;
                OnPropertyChanged(nameof(EventPanelViewModel));
            }
        }

        /// <summary>
        /// ViewModel для панели резервуаров
        /// </summary>
        public BaseViewModel TanksPanelViewModel
        {
            get => _tanksPanelViewModel;
            private set
            {
                _tanksPanelViewModel = value;
                OnPropertyChanged(nameof(TanksPanelViewModel));
            }
        }

        /// <summary>
        /// ViewModel для кассы
        /// </summary>
        public BaseViewModel CashViewModel
        {
            get => _cashViewModel;
            private set
            {
                _cashViewModel = value;
                OnPropertyChanged(nameof(CashViewModel));
            }
        }

        /// <summary>
        /// ViewModel для счетчика пистолетов
        /// </summary>
        public BaseViewModel NozzleCounterPanelViewModel
        {
            get => _nozzleCounterPanelViewModel;
            private set
            {
                _nozzleCounterPanelViewModel = value;
                OnPropertyChanged(nameof(NozzleCounterPanelViewModel));
            }
        }

        /// <summary>
        /// ViewModel для незарегистрированных продаж
        /// </summary>
        public BaseViewModel UnregisteredSalePanelViewModel
        {
            get => _unregisteredSalePanelViewModel;
            private set
            {
                _unregisteredSalePanelViewModel = value;
                OnPropertyChanged(nameof(UnregisteredSalePanelViewModel));
            }
        }
        public DockLayoutManager MainDockLayoutManager { get; set; }
        public bool IsCurrentUserAdmin => _userStore.CurrentUser?.UserType == UserType.Admin;

        #endregion

        #region Constructor

        public MainViewModel(INavigator navigator,
            ILogger<MainViewModel> logger,
            IShiftStore shiftStore,
            IUserStore userStore,
            IFuelSaleService fuelSaleService,
            IUnregisteredSaleService unregisteredSaleService,
            IAuthenticator authenticator,
            ICustomSplashScreenService customSplashScreenService,
            ICashRegisterStore cashRegisterStore,
            INotificationStore notificationStore)
        {
            _navigator = navigator;
            _logger = logger;
            _shiftStore = shiftStore;
            _userStore = userStore;
            _fuelSaleService = fuelSaleService;
            _unregisteredSaleService = unregisteredSaleService;
            _authenticator = authenticator;
            _splashScreenService = customSplashScreenService;
            _cashRegisterStore = cashRegisterStore;
            _notificationStore = notificationStore;
            
            _notificationStore.OnShowing += NotificationStore_OnShowing;
            _userStore.OnLogin += UserStore_OnLogin;
        }

        #endregion

        #region Public Voids

        [Command]
        public async Task WorkplaceSettings()
        {
            try
            {
                WindowService.Title = "Настройки рабочего места";
                WindowService.Show(nameof(WorkplaceSettingsView), await _navigator.GetViewModelAsync(ViewType.WorkPlaceView));
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        [Command]
        public async Task StartWebService()
        {
            try { await Services.ServiceManager.StartWebAsync(); }
            catch (Exception ex) { _logger.LogError(ex, ex.Message); }
        }

        [Command]
        public async Task StopWebService()
        {
            try { await Services.ServiceManager.StopWebAsync(); }
            catch (Exception ex) { _logger.LogError(ex, ex.Message); }
        }

        [Command]
        public async Task StartWorkerService()
        {
            try { await Services.ServiceManager.StartWorkerAsync(); }
            catch (Exception ex) { _logger.LogError(ex, ex.Message); }
        }

        [Command]
        public async Task StopWorkerService()
        {
            try { await Services.ServiceManager.StopWorkerAsync(); }
            catch (Exception ex) { _logger.LogError(ex, ex.Message); }
        }

        [Command]
        public async Task RestartAllServices()
        {
            try
            {
                await Services.ServiceManager.StopWorkerAsync();
                await Services.ServiceManager.StopWebAsync();
                await Services.ServiceManager.StartWebAsync();
                await Services.ServiceManager.StartWorkerAsync();
            }
            catch (Exception ex) { _logger.LogError(ex, ex.Message); }
        }

        [Command]
        public void SaveDockLayoutManagerPosition()
        {
            try
            {
                const string LayoutFileName = "layout.xml";
                MainDockLayoutManager?.SaveLayoutToXml(LayoutFileName);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        [Command]
        public void RestoreDockLayoutManagerPosition()
        {
            try
            {
                string? path = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\layout.xml";
                MainDockLayoutManager?.RestoreLayoutFromXml(path);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        [Command]
        public void DockLayoutManagerLoaded(RoutedEventArgs args)
        {
            try
            {
                if (args.Source is DockLayoutManager dockLayoutManager)
                {
                    MainDockLayoutManager = dockLayoutManager;

                    string? path = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\layout.xml";
                    MainDockLayoutManager?.RestoreLayoutFromXml(path);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        [Command]
        public void CurrentDocumentPanelLoaded(RoutedEventArgs args)
        {
            if (args.Source is DocumentPanel documentPanel)
            {
                
            }
        }

        /// <summary>
        /// Управления конфигурацией
        /// </summary>
        [Command]
        public async Task ConfigurationManagement()
        {
            try
            {
                if (_userStore.CurrentUser != null && _userStore.CurrentUser.UserType == UserType.Admin)
                {
                    await ShowDocumentViewerAsync(ViewType.ConfigurationManagementView,
                        nameof(ConfigurationManagementView),
                        "Управления конфигурацией");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        /// <summary>
        /// Управления скидками
        /// </summary>
        [Command]
        public async Task DiscountManagement()
        {
            try
            {
                if (_userStore.CurrentUser != null && _userStore.CurrentUser.UserType == UserType.Admin)
                {
                    await ShowDocumentViewerAsync(ViewType.DiscountManagement,
                        nameof(DiscountManagementView),
                        "Управления скидками");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        /// <summary>
        /// Прием топлива
        /// </summary>
        [Command]
        public async Task FuelIntake()
        {
            try
            {
                if (IsShiftOpen())
                {
                    WindowService.Title = "Прием топлива";
                    WindowService.Show(nameof(FuelIntakeDetailView), await _navigator.GetViewModelAsync(ViewType.FuelIntakeDetailView));
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        /// <summary>
        /// Незавершенные продажи
        /// </summary>
        [Command]
        public async Task UncompletedSales()
        {
            try
            {
                WindowService.Title = "Незавершённые продажи";
                WindowService.Show(nameof(UncompletedSalesView), await _navigator.GetViewModelAsync(ViewType.UncompletedSalesView));
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        /// <summary>
        /// Завершенные продажи
        /// </summary>
        [Command]
        public async Task CompletedSales()
        {
            try
            {
                WindowService.Title = "Завершенные продажи";
                WindowService.Show(nameof(CompletedSalesView), await _navigator.GetViewModelAsync(ViewType.CompletedSalesView));
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        /// <summary>
        /// Переоценка топлива
        /// </summary>
        [Command]
        public async Task Revaluation()
        {
            try
            {
                if (IsShiftOpen())
                {
                    WindowService.Title = "Переоценка топлива";
                    WindowService.Show(nameof(RevaluationView), await _navigator.GetViewModelAsync(ViewType.RevaluationView));
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        /// <summary>
        /// Выход из системы
        /// </summary>
        [Command]
        public void Logout()
        {
            try
            {
                if (MessageBoxService.ShowMessage("Выйти из системы?", "Выход", MessageButton.YesNo) == MessageResult.Yes)
                {
                    _authenticator.Logout();
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        [Command]
        public async Task GlobalReport()
        {
            try
            {
                await ShowDocumentViewerAsync(ViewType.GlobalReportView,
                    nameof(GlobalReportView),
                    "Глобальные отчеты");
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        [Command]
        public void WindowLoaded()
        {
            try
            {
                WindowService?.Activate();
                DocumentViewerService?.Activate();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        [Command]
        public void CloseApplication()
        {
            var result = MessageBoxService.ShowMessage("Вы уверены, что хотите закрыть программу?", "Подтверждение", MessageButton.YesNo, MessageIcon.Question);
            if (result == MessageResult.Yes)
            {
                Application.Current.Shutdown();
            }
        }

        #endregion

        #region ККМ

        /// <summary>
        /// Закрыть смену
        /// </summary>
        [Command]
        public async Task CloseShift()
        {
            try
            {
                if (await CanShiftClose())
                {
                    var result = MessageBoxService.ShowMessage("Закрыть смену?", "Закрытие смены", MessageButton.YesNo, MessageIcon.Question);
                    if (result == MessageResult.Yes)
                    {
                        await _shiftStore.CloseShiftAsync();
                        await CashRegisterCloseShift();
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        /// <summary>
        /// Открыть смену
        /// </summary>
        /// <returns></returns>
        [Command]
        public async Task OpenShift()
        {
            try
            {
                if (CanShiftOpen())
                {
                    var result = MessageBoxService.ShowMessage("Открыть смену?", "Открытие смены", MessageButton.YesNo, MessageIcon.Question);
                    if (result == MessageResult.Yes)
                    {
                        await _shiftStore.OpenShiftAsync();
                        await CashRegisterOpenShift();
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        /// <summary>
        /// Закрыть смену ККМ (Z-отчет)
        /// </summary>
        /// <returns></returns>
        [Command]
        public async Task CashRegisterCloseShift()
        {
            try
            {
                var result = MessageBoxService.ShowMessage("Закрыть смену ККМ?", "Закрытие смены", MessageButton.YesNo, MessageIcon.Question);
                if (result == MessageResult.No) return;

                _splashScreenService.Show("Закрытие смены ККМ...");
                await _cashRegisterStore.CloseShiftAsync();
            }
            catch (CashRegisterException e)
            {
                MessageBoxService.ShowMessage(e.Message, "Ошибка", MessageButton.OK, MessageIcon.Error);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
            finally
            {
                _splashScreenService.Close();
            }
        }

        /// <summary>
        /// Открыть смену
        /// </summary>
        /// <returns></returns>
        [Command]
        public async Task CashRegisterOpenShift()
        {
            try
            {
                var result = MessageBoxService.ShowMessage("Открыть смену ККМ?", "Открытие смены", MessageButton.YesNo, MessageIcon.Question);
                if (result == MessageResult.No) return;

                _splashScreenService.Show("Открытие смены ККМ...");
                await _cashRegisterStore.OpenShiftAsync();
            }
            catch (CashRegisterException e)
            {
                MessageBoxService.ShowMessage(e.Message, "Ошибка", MessageButton.OK, MessageIcon.Error);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
            finally
            {
                _splashScreenService.Close();
            }
        }

        /// <summary>
        /// Х-отчет
        /// </summary>
        /// <returns></returns>
        [Command]
        public async Task CashRegisterXReport()
        {
            try
            {
                _splashScreenService.Show("X-отчет ККМ...");
                await _cashRegisterStore.XReportAsync();
            }
            catch (CashRegisterException e)
            {
                MessageBoxService.ShowMessage(e.Message, "Ошибка", MessageButton.OK, MessageIcon.Error);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
            finally
            {
                _splashScreenService.Close();
            }
        }

        /// <summary>
        /// Получение состояние ККМ
        /// </summary>
        /// <returns></returns>
        [Command]
        public async Task CashRegisterGetState()
        {
            try
            {
                _splashScreenService.Show("Получение состояние ККМ...");
                var state = await _cashRegisterStore.GetShiftStateAsync();

                switch (state.Status)
                {
                    case CashRegisterStatus.Unknown:
                        MessageBoxService.ShowMessage("Статус ККМ неизвестен. Проверьте работу ККМ.", "Внимание!", MessageButton.OK, MessageIcon.Warning);
                        break;
                    case CashRegisterStatus.Open:
                        MessageBoxService.ShowMessage("Смена ККМ открыта.", "Информация", MessageButton.OK, MessageIcon.Information);
                        break;
                    case CashRegisterStatus.Close:
                        MessageBoxService.ShowMessage("Смена ККМ закрыта.", "Информация", MessageButton.OK, MessageIcon.Information);
                        break;
                    case CashRegisterStatus.Exceeded24Hours:
                        MessageBoxService.ShowMessage("Смена ККМ превысила 24 часа. Необходимо закрыть смену ККМ.", "Внимание!", MessageButton.OK, MessageIcon.Warning);
                        break;
                }
            }
            catch (CashRegisterException e)
            {
                MessageBoxService.ShowMessage(e.Message, "Ошибка", MessageButton.OK, MessageIcon.Error);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
            finally
            {
                _splashScreenService.Close();
            }
        }

        /// <summary>
        /// Произвольный чек
        /// </summary>
        /// <returns></returns>
        [Command]
        public async Task CashRegisterCustomReceipt()
        {
            try
            {
                if (IsShiftOpen())
                {

                    //if (_cashRegisterManager.CurrentCashRegister == null) throw new CashRegisterManagerException("Не добавлена касса ККМ.");

                    WindowService.Title = "Произвольный чек";
                    WindowService.Show(nameof(CustomReceiptView), await _navigator.GetViewModelAsync(ViewType.CustomReceiptView));
                }
            }
            //catch (CashRegisterException e)
            //{
            //    MessageBoxService.ShowMessage(e.Message, "Ошибка", MessageButton.OK, MessageIcon.Error);
            //}
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        #endregion

        #region Private Voids

        private void UserStore_OnLogin(User user)
        {
            OnPropertyChanged(nameof(IsCurrentUserAdmin));
        }

        private async Task<bool> CheckUncompletedSale()
        {
            try
            {
                var uncompletedSales = await _fuelSaleService.GetUncompletedFuelSaleAsync(_shiftStore.CurrentShift.Id);
                return uncompletedSales == null || !uncompletedSales.Any();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error checking uncompleted sales: {Message}", e.Message);
                return false;
            }
        }

        private async Task<bool> CheckUnregisteredSales()
        {
            try
            {
                var unregisteredSales = await _unregisteredSaleService.GetUnregisteredSales();
                return unregisteredSales == null || !unregisteredSales.Any();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error checking unregistered sales: {Message}", e.Message);
                return false;
            }
        }

        private async Task<bool> CanShiftClose()
        {
            if (_shiftStore.CurrentShift == null)
            {
                MessageBoxService.ShowMessage("Смена не открыта!", "Информация", MessageButton.OK, MessageIcon.Information);
                return false;
            }
            if (_shiftStore.CurrentShiftState == ShiftState.Closed)
            {
                MessageBoxService.ShowMessage("Смена уже закрыта!", "Информация", MessageButton.OK, MessageIcon.Information);
                return false;
            }
            if (!await CheckUncompletedSale())
            {
                MessageBoxService.ShowMessage("Есть незавершенные продажи!", "Закрытие смены", MessageButton.OK, MessageIcon.Exclamation);
                return false;
            }
            if (!await CheckUnregisteredSales())
            {
                MessageBoxService.ShowMessage("Есть незарегистрированные отпуски топлива!", "Закрытие смены", MessageButton.OK, MessageIcon.Exclamation);
                return false;
            }
            return true;
        }

        private bool CanShiftOpen()
        {
            if (_shiftStore.CurrentShift == null)
            {
                return true;
            }

            if (_shiftStore.CurrentShiftState is ShiftState.Open or ShiftState.Exceeded24Hours)
            {
                MessageBoxService.ShowMessage("Смена уже открыта!", "Информация", MessageButton.OK, MessageIcon.Information);
                return false;
            }

            return true;
        }

        private bool IsShiftOpen()
        {
            if (_shiftStore.CurrentShift == null)
            {
                MessageBoxService.ShowMessage("Смена не открыта!", "Информация", MessageButton.OK, MessageIcon.Information);
                return false;
            }

            if (_shiftStore.CurrentShiftState == ShiftState.Closed)
            {
                MessageBoxService.ShowMessage("Смена закрыта!", "Информация", MessageButton.OK, MessageIcon.Information);
                return false;
            }

            if (_shiftStore.CurrentShiftState == ShiftState.Exceeded24Hours)
            {
                MessageBoxService.ShowMessage("Смена превысила 24 часа, закройте смену!", "Информация", MessageButton.OK, MessageIcon.Information);
                return false;
            }

            return _shiftStore.CurrentShiftState == ShiftState.Open;
        }

        private bool IsShiftClosed()
        {
            if (_shiftStore.CurrentShift == null)
            {
                return true;
            }

            if (_shiftStore.CurrentShiftState == ShiftState.Closed)
            {
                return true;
            }
            return false;
        }

        #endregion

        #region Helpers

        private async Task ShowDocumentViewerAsync(ViewType viewType, string viewName, string title)
        {
            if (!_documentViewModels.TryGetValue(viewType, out var weakReference)
                || !weakReference.TryGetTarget(out var viewModel)
                || viewModel == null)
            {
                viewModel = await _navigator.GetViewModelAsync(viewType);
                _documentViewModels[viewType] = new WeakReference<BaseViewModel>(viewModel);
            }

            viewModel.Title = title;
            DocumentViewerService.Show(viewName, viewModel);
            DocumentViewerService.Activate();
        }

        #endregion

        #region Initializable

        public async Task StartAsync()
        {
            TanksPanelViewModel = await _navigator.GetViewModelAsync(ViewType.TanksPanelView);
            FuelSaleViewModel = await _navigator.GetViewModelAsync(ViewType.FuelSaleView);
            CurrentViewModel = await _navigator.GetViewModelAsync(ViewType.ControllerListView);
            EventPanelViewModel = await _navigator.GetViewModelAsync(ViewType.EventPanelView);
            CashViewModel = await _navigator.GetViewModelAsync(ViewType.CashView);
            NozzleCounterPanelViewModel = await _navigator.GetViewModelAsync(ViewType.NozzleCounterPanelView);
            UnregisteredSalePanelViewModel = await _navigator.GetViewModelAsync(ViewType.UnregisteredSalePanelView);
        }

        #endregion

        #region Notification

        private void NotificationStore_OnShowing(string title, string message, NotificationType type)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                CustomToastViewModel viewModel = new()
                {
                    Title = title,
                    Message = message,
                    Type = type
                };
                INotification notification = NotificationService.CreateCustomNotification(viewModel);
                notification.ShowAsync();
            });
        }

        #endregion

        #region Dispose

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _notificationStore.OnShowing -= NotificationStore_OnShowing;
                _userStore.OnLogin -= UserStore_OnLogin;
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}
