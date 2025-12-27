using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using KIT.GasStation.Hardware.Services;
using KIT.GasStation.Hardware.State.Navigators;
using KIT.GasStation.Hardware.Views;
using KIT.GasStation.HardwareConfigurations.Models;
using KIT.GasStation.HardwareConfigurations.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace KIT.GasStation.Hardware.ViewModels
{
    public class MainWindowViewModel : BaseViewModel
    {
        #region Private Members

        private BaseViewModel? _selectedBaseViewModel;
        private readonly IHardwareConfigurationService _hardwareConfigurationService;
        private readonly INavigator _navigator;
        private ObservableCollection<Controller> _controllers = new();
        private ObservableCollection<CashRegister> _cashRegisters = new();
        private Controller _selectedController;
        private CashRegister _selectedCashRegister;

        #endregion

        #region Public Properties

        public ObservableCollection<Controller> Controllers
        {
            get => _controllers;
            set
            {
                _controllers = value;
                OnPropertyChanged(nameof(Controllers));
            }
        }
        public ObservableCollection<CashRegister> CashRegisters
        {
            get => _cashRegisters;
            set
            {
                _cashRegisters = value;
                OnPropertyChanged(nameof(CashRegisters));
            }
        }
        public BaseViewModel? SelectedBaseViewModel
        {
            get => _selectedBaseViewModel;
            set
            {
                _selectedBaseViewModel = value;
                OnPropertyChanged(nameof(SelectedBaseViewModel));
            }
        }
        public Controller SelectedController
        {
            get => _selectedController;
            set
            {
                _selectedController = value;
                OnPropertyChanged(nameof(SelectedController));
            }
        }
        public CashRegister SelectedCashRegister
        {
            get => _selectedCashRegister;
            set
            {
                _selectedCashRegister = value;
                OnPropertyChanged(nameof(SelectedCashRegister));
            }
        }

        #endregion

        #region Constructors

        public MainWindowViewModel(INavigator navigator,
            IHardwareConfigurationService hardwareConfigurationService)
        {
            _hardwareConfigurationService = hardwareConfigurationService;
            _navigator = navigator;

            _hardwareConfigurationService.OnControllerPropertyChanged += OnControllerPropertyChanged;
            _hardwareConfigurationService.OnCashRegisterPropertyChanged += OnCashRegisterPropertyChanged;

            InitializeConfigurationAsync();
        }

        #endregion

        #region Commands

        [Command]
        public void ControllerCreate()
        {
            IDeviceService<Controller> controllerService = new ControllerService(_hardwareConfigurationService);
            var viewModel = new DeviceViewModel<Controller, ControllerType>(controllerService);
            WindowService.Title = "Создание ТРК";
            WindowService.Show(nameof(ControllerView), viewModel);
        }

        [Command]
        public void CashRegisterCreate()
        {
            IDeviceService<CashRegister> cashRegisterService = new CashRegisterService(_hardwareConfigurationService);
            var viewModel = new DeviceViewModel<CashRegister, CashRegisterType>(cashRegisterService);
            WindowService.Title = "Создание кассы";
            WindowService.Show(nameof(ControllerView), viewModel);
        }

        [Command]
        public void TreeViewSelectedItemChanged(RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is Controller controller)
            {
                ChangeBaseViewModel(controller);
                return;
            }
            if (e.NewValue is CashRegister cashRegister)
            {
                ChangeBaseViewModel(cashRegister);
                return;
            }
            SelectedBaseViewModel = null;
        }

        [Command]
        public async Task FuelTransferColumnDelete()
        {
            if (SelectedController == null)
            {
                return;
            }

            var result = MessageBoxService.ShowMessage($"Удалить выбранный ТРК? \"{SelectedController.Name}\"", "Подтверждение", MessageButton.YesNo, MessageIcon.Question);

            if (result == MessageResult.Yes)
            {
                if (await _hardwareConfigurationService.RemoveControllerAsync(SelectedController.Id))
                {
                    Controllers.Remove(SelectedController);
                }
                else
                {
                    MessageBoxService.ShowMessage("Ошибка удаления ТРК", "Ошибка", MessageButton.OK, MessageIcon.Error);
                }
            }
        }

        [Command]
        public async Task CashRegisterDelete()
        {
            if (SelectedCashRegister == null)
            {
                return;
            }
            var result = MessageBoxService.ShowMessage($"Удалить выбранную кассу? \"{SelectedCashRegister.Name}\"", "Подтверждение", MessageButton.YesNo, MessageIcon.Question);
            if (result == MessageResult.Yes)
            {
                if (await _hardwareConfigurationService.RemoveCashRegisterAsync(SelectedCashRegister.Id))
                {
                    CashRegisters.Remove(SelectedCashRegister);
                }
                else
                {
                    MessageBoxService.ShowMessage("Ошибка удаления кассы", "Ошибка", MessageButton.OK, MessageIcon.Error);
                }
            }
        }

        [Command]
        public void MouseRightButtonDown(MouseButtonEventArgs e)
        {
            var treeViewItem = VisualUpwardSearch(e.OriginalSource as DependencyObject);
            if (treeViewItem != null)
            {
                if (treeViewItem.DataContext is Controller controller)
                {
                    SelectedController = controller;
                }
                if (treeViewItem.DataContext is CashRegister cashRegister)
                {
                    SelectedCashRegister = cashRegister;
                }
            }
        }

        [Command]
        public void WindowLoaded()
        {
            try
            {
                WindowService?.Activate();
            }
            catch (Exception)
            {

            }
        }

        [Command]
        public void KITCreate()
        {
            SelectedBaseViewModel = new KITViewModel();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Асинхронно инициализирует конфигурацию оборудования.
        /// </summary>
        private async void InitializeConfigurationAsync()
        {
            try
            {
                // Проверяем существование файла конфигурации и создаём новый, если его нет.
                await _hardwareConfigurationService.EnsureConfigurationFileExistsAsync();

                // Читаем конфигурацию из файла. Получаем коллекцию контроллеров.
                Controllers = await _hardwareConfigurationService.GetControllersAsync();
                CashRegisters = await _hardwareConfigurationService.GetCashRegistersAsync();
                Console.WriteLine("Конфигурация успешно загружена.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка инициализации конфигурации: {ex.Message}");
            }
        }
        private void ChangeBaseViewModel(Controller controller)
        {
            SelectedController = controller;
            switch (controller.Type)
            {
                case ControllerType.None:
                    break;
                case ControllerType.Lanfeng:
                    LanfengViewModel lanfengViewModel = (LanfengViewModel)_navigator.GetViewModel(ViewType.Lanfeng);
                    lanfengViewModel.SelectedController = controller;
                    SelectedBaseViewModel = lanfengViewModel;
                    break;
                case ControllerType.Gilbarco:
                    break;
                case ControllerType.Emulator:
                    break;
                case ControllerType.PKElectronics:
                    PKElectronicsViewModel pKElectronicsViewModel = (PKElectronicsViewModel)_navigator.GetViewModel(ViewType.PKElectronics);
                    pKElectronicsViewModel.SelectedController = controller;
                    SelectedBaseViewModel = pKElectronicsViewModel;
                    break;
                case ControllerType.TechnoProjekt:
                    break;
                default:
                    break;
            }
        }
        private void ChangeBaseViewModel(CashRegister cashRegister)
        {
            SelectedCashRegister = cashRegister;
            switch (cashRegister.Type)
            {
                case CashRegisterType.None:
                    break;
                case CashRegisterType.EKassa:
                    EKassaViewModel eKassaViewModel = (EKassaViewModel)_navigator.GetViewModel(ViewType.EKassa);
                    eKassaViewModel.SelectedCashRegister = cashRegister;
                    SelectedBaseViewModel = eKassaViewModel;
                    break;
                case CashRegisterType.NewCas:
                    NewCasViewModel newCasViewModel = (NewCasViewModel)_navigator.GetViewModel(ViewType.NewCas);
                    newCasViewModel.SelectedCashRegister = cashRegister;
                    SelectedBaseViewModel = newCasViewModel;
                    break;
                case CashRegisterType.MF:
                    break;
                default:
                    break;
            }
        }
        private void OnControllerPropertyChanged(Controller controller)
        {
            Controller? existingController = Controllers.FirstOrDefault(c => c.Id == controller.Id);
            if (existingController != null)
            {
                existingController.Update(controller);
            }
            else
            {
                Controllers.Add(controller);
            }
        }
        private void OnCashRegisterPropertyChanged(CashRegister cashRegister)
        {
            CashRegister? existingCashRegister = CashRegisters.FirstOrDefault(c => c.Id == cashRegister.Id);
            if (existingCashRegister != null)
            {
                existingCashRegister.Update(cashRegister);
            }
            else
            {
                CashRegisters.Add(cashRegister);
            }
        }
        private static TreeViewItem VisualUpwardSearch(DependencyObject source)
        {
            while (source != null && !(source is TreeViewItem))
            {
                source = VisualTreeHelper.GetParent(source);
            }
            return source as TreeViewItem;
        }

        #endregion
    }
}
