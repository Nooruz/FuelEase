using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Xpf.Docking;
using KIT.GasStation.CashRegisters.Exceptions;
using KIT.GasStation.Domain.Models;
using KIT.GasStation.Domain.Services;
using KIT.GasStation.SplashScreen;
using KIT.GasStation.State.Authenticators;
using KIT.GasStation.State.CashRegisters;
using KIT.GasStation.State.Navigators;
using KIT.GasStation.State.Notifications;
using KIT.GasStation.State.Shifts;
using KIT.GasStation.State.Users;
using KIT.GasStation.ViewModels.Base;
using KIT.GasStation.ViewModels.Factories;
using KIT.GasStation.ViewModels.Info;
using KIT.GasStation.Views;
using KIT.GasStation.Views.Details;
using KIT.GasStation.Views.Info;
using Microsoft.Extensions.Logging;
using System;
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
            

            _shiftStore.OnClosed += (shift) => ShowOpenShiftView();
            _shiftStore.OnLogin += (shift) => ShowOpenShiftView();
            _notificationStore.OnShowing += NotificationStore_OnShowing;
            _cashRegisterStore.OnShiftOpened += CashRegisterStore_OnShiftOpened;
            _cashRegisterStore.OnShiftClosed += CashRegisterStore_OnShiftClosed;
            //_cashRegisterStore.OnStatusChanged += CashRegisterStore_OnStatusChanged;
            _cashRegisterStore.OnUnknownError += CashRegisterStore_OnUnknownError;

            _fuelSaleService.OnCreated += FuelSaleService_OnCreated;

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
                    DocumentViewerService.Title = "Управления конфигурацией";
                    DocumentViewerService.Show(nameof(ConfigurationManagementView), await _navigator.GetViewModelAsync(ViewType.ConfigurationManagementView));
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
                    DocumentViewerService.Title = "Управления скидками";
                    DocumentViewerService.Show(nameof(DiscountManagementView), await _navigator.GetViewModelAsync(ViewType.DiscountManagement));
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
                WindowService.Title = "Незавершенные продажи";
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
                var viewModel = await _navigator.GetViewModelAsync(ViewType.GlobalReportView);
                DocumentViewerService.Title = "Глобальные отчеты";
                DocumentViewerService.Show(nameof(GlobalReportView), viewModel);
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
                string? state = await _cashRegisterStore.GetShiftStateAsync();

                if (string.IsNullOrEmpty(state))
                {
                    return;
                }

                var viewModel = new CashRegisterStateInfoViewModel(state);

                WindowService.Title = "Состояние ККМ";
                WindowService.Show(nameof(CashRegisterStateInfoView), viewModel);
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

        private async Task GetViewModelAsync(ViewType viewType)
        {
            await _navigator.GetViewModelAsync(viewType);
        }

        //private void CashRegisterManager_OnShiftClose(CashRegister cashRegister)
        //{
        //    Application.Current.Dispatcher.Invoke(() =>
        //    {
        //        ShowNotification("Информация", "Смена ККМ закрыта.");
        //    });
        //}

        //private void CashRegisterManager_OnCashRegisterState(CashRegisterType type, string state)
        //{
        //    try
        //    {
        //        MessageBoxService.ShowMessage(state, $"Инормация по {EnumHelper.GetEnumDisplayName(type)}", MessageButton.OK, MessageIcon.Information);
        //    }
        //    catch (Exception e)
        //    {
        //        _logger?.LogError(e, e.Message);
        //    }
        //}

        //private void CashRegisterManager_OnError(CashRegister cashRegister, string message)
        //{
        //    Application.Current.Dispatcher.Invoke(() =>
        //    {
        //        ShowNotification("Информация", message);
        //    });
        //}

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

        private void ShowOpenShiftView()
        {
            //var viewModel = new OpenShiftViewModel(_shiftStore,
            //_splashScreenService);

            //if (_shiftStore == null) return;

            //if (_shiftStore.CurrentShift == null)
            //{
            //    DialogService.ShowDialog(MessageButton.OK, "Смена закрыта", "Смена не открыта", MessageIcon.Information);
            //    windowService.Show(nameof(OpenShiftView), viewModel);
            //    // Запускаем цикл обработки сообщений
            //    System.Windows.Threading.Dispatcher.Run();
            //    return;
            //}

            //if (_shiftStore.CurrentShiftState is ShiftState.Exceeded24Hours or ShiftState.Closed)
            //{
            //    windowService.Show(nameof(OpenShiftView), viewModel);
            //    // Запускаем цикл обработки сообщений
            //    System.Windows.Threading.Dispatcher.Run();
            //    return;
            //}

            //Thread uiThread = new(() =>
            //{
            //    try
            //    {
            //        IWindowService windowService = new WindowService()
            //        {
            //            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            //            AllowSetWindowOwner = true,
            //            WindowShowMode = WindowShowMode.Dialog,
            //            Name = "DialogService",
            //            WindowStyle = new Style
            //            {
            //                TargetType = typeof(ThemedWindow),
            //                BasedOn = Application.Current.TryFindResource("DialogService") as Style
            //            }
            //        };

            //        if (windowService == null) return;

            //        var viewModel = new OpenShiftViewModel(_shiftStore,
            //        _splashScreenService);
            //        windowService.Title = "Смена закрыта";

            //        if (windowService.IsWindowAlive) return;

            //        if (_shiftStore == null) return;

            //        if (_shiftStore.CurrentShift == null)
            //        {
            //            windowService.Show(nameof(OpenShiftView), viewModel);
            //            // Запускаем цикл обработки сообщений
            //            System.Windows.Threading.Dispatcher.Run();
            //            return;
            //        }

            //        if (_shiftStore.CurrentShiftState is ShiftState.Exceeded24Hours or ShiftState.Closed)
            //        {
            //            windowService.Show(nameof(OpenShiftView), viewModel);
            //            // Запускаем цикл обработки сообщений
            //            System.Windows.Threading.Dispatcher.Run();
            //            return;
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        _logger.LogError(ex, ex.Message);
            //    }
            //});
            //uiThread.SetApartmentState(ApartmentState.STA); // Обязательно устанавливаем STA
            //uiThread.Start();
        }

        private void CashRegisterStore_OnShiftClosed()
        {
            MessageBoxService.ShowMessage("Смена на ККМ успешно закрыта. Не забудьте открыть новую смену перед началом работы.", 
                "Смена закрыта",
                MessageButton.OK,
                MessageIcon.Information);
        }

        private void CashRegisterStore_OnShiftOpened()
        {
            MessageBoxService.ShowMessage("Смена на ККМ успешно открыта.",
                "Смена открыта",
                MessageButton.OK,
                MessageIcon.Information);
        }

        //private void CashRegisterStore_OnStatusChanged(CashRegisterStatus status)
        //{
        //    //string? message = status switch
        //    //{
        //    //    CashRegisterStatus.Exceeded24Hours => "Смена на ККМ открыта более 24 часов. Пожалуйста, закройте смену и откройте новую.",
        //    //    CashRegisterStatus.Close => "Смена на ККМ закрыта. Пожалуйста, откройте новую смену перед началом работы.",
        //    //    CashRegisterStatus.Error => "Ошибка ККМ. Проверьте соединение с сервером или настройки кассы.",
        //    //    CashRegisterStatus.Unknown => "Статус ККМ неизвестен. Проверьте работу ККМ.",
        //    //    CashRegisterStatus.NoOpenedShift => "Смена на ККМ не открыта. Откройте смену перед началом работы.",
        //    //    _ => null
        //    //};

        //    //if (message != null)
        //    //{
        //    //    _ = MessageBoxService.ShowMessage(message, "Внимание!", MessageButton.OK, MessageIcon.Warning);
        //    //}
        //}

        private void CashRegisterStore_OnUnknownError(string errorMessage)
        {

            _ = MessageBoxService.ShowMessage(errorMessage, "Внимание!", MessageButton.OK, MessageIcon.Error);
        }

        private async void FuelSaleService_OnCreated(FuelSale fuelSale)
        {
            _splashScreenService.Show("Идёт оформление топлива...");

            // Даём пользователю увидеть splash
            await Task.Delay(2000);


            _splashScreenService.Close();
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
    }
}
