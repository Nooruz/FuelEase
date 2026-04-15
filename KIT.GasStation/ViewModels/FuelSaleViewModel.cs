using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Xpf.Editors;
using KIT.GasStation.Domain.Models;
using KIT.GasStation.Domain.Services;
using KIT.GasStation.Domain.Views;
using KIT.GasStation.HardwareConfigurations.Models;
using KIT.GasStation.Helpers;
using KIT.GasStation.Services;
using KIT.GasStation.SplashScreen;
using KIT.GasStation.State.CashRegisters;
using KIT.GasStation.State.Discounts;
using KIT.GasStation.State.Nozzles;
using KIT.GasStation.State.Shifts;
using KIT.GasStation.ViewModels.Base;
using KIT.GasStation.Views;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace KIT.GasStation.ViewModels
{
    public class FuelSaleViewModel : PanelViewModel
    {
        #region Private Members

        private readonly IFuelSaleService _fuelSaleService;
        private readonly IFiscalDataService _fiscalDataService;
        private readonly INozzleStore _nozzleStore;
        private readonly IViewService<TankFuelQuantityView> _tankFuelQuantityView;
        private readonly ILogger<FuelSaleViewModel> _logger;
        private readonly IFuelService _fuelService;
        private readonly IShiftStore _shiftStore;
        private readonly IDisсountStore _disсountStore;
        private readonly ICashRegisterStore _cashRegisterStore;
        private readonly IHotKeysService _hotKeysService;
        private readonly ICustomSplashScreenService _splashScreenService;
        private int? _tube;
        private Nozzle? _selectedNozzle;
        private FuelSale _createFuelSale = new() { PaymentType = PaymentType.Cash };
        private bool _isUpdatingQuantity;
        private bool _isUpdatingSum;
        private bool _isSumUpdating;
        private decimal _sum;
        private decimal _quantity;
        private TextEdit _sumTextEdit;
        private TextEdit _tubeTextEdit;

        #endregion

        #region Public Properties

        public List<KeyValuePair<PaymentType, string>> PaymentTypes => new(EnumHelper.GetLocalizedEnumValues<PaymentType>());
        public ObservableCollection<Nozzle> Nozzles => _nozzleStore.Nozzles;
        public int? Tube
        {
            get => _tube;
            set
            {
                _tube = value;
                OnPropertyChanged(nameof(Tube));
            }
        }
        public Nozzle? SelectedNozzle
        {
            get => _selectedNozzle;
            set
            {
                _selectedNozzle = value;
                OnPropertyChanged(nameof(SelectedNozzle));
                NozzleSelectionChanged();
            }
        }
        public FuelSale CreateFuelSale
        {
            get => _createFuelSale;
            set
            {
                _createFuelSale = value;
                OnPropertyChanged(nameof(CreateFuelSale));
            }
        }
        public decimal Sum
        {
            get => _sum;
            set
            {
                if (_sum == value)
                {
                    return;
                }

                _sum = value;
                OnPropertyChanged(nameof(Sum));
                decimal? price = SelectedNozzle?.Price;
                if (!_isUpdatingQuantity && price is > 0)
                {
                    _isSumUpdating = true;
                    _isUpdatingSum = true;
                    Quantity = decimal.Floor((Sum / price.Value) * 100) / 100.0m;
                    _isUpdatingSum = false;
                }
            }
        }
        public decimal Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity == value)
                {
                    return;
                }
                _quantity = value;
                OnPropertyChanged(nameof(Quantity));
                if (!_isUpdatingSum && SelectedNozzle?.Price is > 0)
                {
                    _isSumUpdating = false;
                    _isUpdatingQuantity = true;
                    Sum = Math.Round(Quantity * SelectedNozzle.Price, 2);
                    _isUpdatingQuantity = false;
                }
            }
        }

        #endregion

        #region Constructor

        public FuelSaleViewModel(INozzleStore nozzleStore,
            IFuelSaleService fuelSaleService,
            IViewService<TankFuelQuantityView> tankFuelQuantityView,
            ILogger<FuelSaleViewModel> logger,
            IFuelService fuelService,
            IShiftStore shiftStore,
            IDisсountStore disсountStore,
            ICashRegisterStore cashRegisterStore,
            IHotKeysService hotKeysService,
            IFiscalDataService fiscalDataService,
            ICustomSplashScreenService splashScreenService)
        {
            _nozzleStore = nozzleStore;
            _tankFuelQuantityView = tankFuelQuantityView;
            _fuelSaleService = fuelSaleService;
            _logger = logger;
            _fuelService = fuelService;
            _shiftStore = shiftStore;
            _disсountStore = disсountStore;
            _cashRegisterStore = cashRegisterStore;
            _hotKeysService = hotKeysService;
            _fiscalDataService = fiscalDataService;
            _splashScreenService = splashScreenService;

            Title = "Панель заявок";

            _fuelService.OnUpdated += FuelService_OnUpdated;
            _fuelSaleService.OnUpdated += FuelSaleService_OnUpdated;
            _shiftStore.OnNozzleSelectionChanged += ShiftStore_OnNozzleSelectionChanged;
            _nozzleStore.OnNozzleSelected += OnNozzleSelected;
            _hotKeysService.OnHotKeyPressed += HotKeysService_OnHotKeyPressed;
        }

        #endregion

        #region Public Voids

        [Command]
        public async Task Sale()
        {
            try
            {
                await OpenPayView();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Ошибка при создании продажи топлива.");
            }
        }

        [Command]
        public void UserControlLoaded()
        {
            try
            {
                WindowService.Activate();
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        [Command]
        public void SumTextEditLoaded(RoutedEventArgs args)
        {
            if (args.Source is TextEdit textEdit)
            {
                _sumTextEdit = textEdit;
            }
        }

        [Command]
        public void TubeTextEditLoading(RoutedEventArgs args)
        {
            if (args.Source is TextEdit textEdit)
            {
                _tubeTextEdit = textEdit;
            }
        }

        #endregion

        #region Hot Keys

        private async void HotKeysService_OnHotKeyPressed(HotKeyAction hotKeyAction)
        {
            switch (hotKeyAction)
            {
                case HotKeyAction.FuelSale:
                    await OnFuelSale();
                    break;
                case HotKeyAction.FuelSaleCashless:
                    CreateFuelSale.PaymentType = PaymentType.Cashless;
                    break;
                case HotKeyAction.FuelSaleCash:
                    CreateFuelSale.PaymentType = PaymentType.Cash;
                    break;
                case HotKeyAction.FuelSaleTicket:
                    CreateFuelSale.PaymentType = PaymentType.Ticket;
                    break;
                case HotKeyAction.StartFullFueling:
                    await StartFullFueling();
                    break;
            }
        }

        #endregion

        #region Private members

        private async Task StartFullFueling()
        {
            try
            {
                Quantity = 100;

                if (!await CanFuelSale())
                {
                    return;
                }

                CreateFuelSale.TankId = SelectedNozzle.TankId;
                CreateFuelSale.ShiftId = _shiftStore.CurrentShift.Id;
                CreateFuelSale.NozzleId = SelectedNozzle.Id;
                CreateFuelSale.Sum = Sum;
                CreateFuelSale.Quantity = Quantity;
                CreateFuelSale.Price = SelectedNozzle.Tank.Fuel.Price;
                CreateFuelSale.DiscountSale = _disсountStore.CalculateDiscount(CreateFuelSale, SelectedNozzle.Tank.Fuel.Id, _isSumUpdating);
                CreateFuelSale.ReceivedCount = SelectedNozzle.LastCounter;
                CreateFuelSale.IsForSum = _isSumUpdating;


                if (Properties.Settings.Default.ReceiptPrintingMode == "Before")
                {
                    var createFiscalData = CreateFuelSale.CreateFiscalData();
                    var fiscalData = await _cashRegisterStore.SaleAsync(createFiscalData);

                    if (fiscalData != null)
                    {
                        await _fuelSaleService.CreateAsync(CreateFuelSale);
                        await _fiscalDataService.CreateAsync(fiscalData);
                    }
                }
                else
                {
                    await _fuelSaleService.CreateAsync(CreateFuelSale);
                }

            }
            catch (Exception)
            {

            }
        }

        private async Task OpenPayView()
        {
            if (!await CanFuelSale())
            {
                return;
            }

            bool hasUncompletedFuelSale = await CheckUncomplatedFuelSaleAsync(SelectedNozzle);

            if (hasUncompletedFuelSale)
            {
                MessageBoxService.ShowMessage("Существует незавершенная продажа топлива по выбранной ТРК. Завершите ее в окне \"Незавершённые продажи\" .", "Внимание", MessageButton.OK, MessageIcon.Exclamation);
                return;
            }

            CreateFuelSale.TankId = SelectedNozzle.TankId;
            CreateFuelSale.ShiftId = _shiftStore.CurrentShift.Id;
            CreateFuelSale.NozzleId = SelectedNozzle.Id;
            CreateFuelSale.Sum = Sum;
            CreateFuelSale.Quantity = Quantity;
            CreateFuelSale.Price = SelectedNozzle.Tank.Fuel.Price;
            CreateFuelSale.DiscountSale = _disсountStore.CalculateDiscount(CreateFuelSale, SelectedNozzle.Tank.Fuel.Id, _isSumUpdating);
            CreateFuelSale.ReceivedCount = SelectedNozzle.LastCounter;
            CreateFuelSale.IsForSum = _isSumUpdating;

            PayViewModel viewModel = new(_fuelSaleService, _disсountStore, _cashRegisterStore, _fiscalDataService, _splashScreenService)
            {
                CreateFuelSale = CreateFuelSale,
                SelectedNozzle = SelectedNozzle,
            };

            WindowService.Title = "Оплата";
            WindowService.Show(nameof(PayView), viewModel);

            CleanForm();
        }

        private async Task OnFuelSale()
        {
            if (Tube == null || Tube == 0)
            {
                _tubeTextEdit?.Dispatcher.BeginInvoke(new Action(() => _tubeTextEdit.Focus()), DispatcherPriority.Background);
            }
            else
            {
                SetFuelNozzle();
            }

            if (Sum > 0)
            {
                await OpenPayView();
            }
        }

        private void SetFuelNozzle()
        {
            if (Tube == null)
            {
                return;
            }

            Nozzle? nozzle = Nozzles.FirstOrDefault(n => n.Tube == Tube);

            if (nozzle != null)
            {
                SelectedNozzle = nozzle;

                _nozzleStore.SelectNozzle(Tube.Value);
            }
            else
            {
                MessageBoxService.ShowMessage("ТРК с таким номером не найдена!", "Внимание", MessageButton.OK, MessageIcon.Exclamation);
            }
        }

        private void FuelService_OnUpdated(Fuel fuel)
        {
            foreach (var item in Nozzles)
            {
                if (item.Tank?.Fuel?.Id == fuel.Id)
                {
                    item.Tank.Fuel.Price = fuel.Price;
                }
            }
            CreateFuelSale.Price = fuel.Price;
            CreateFuelSale.Sum = CreateFuelSale.Quantity * fuel.Price;
            if (SelectedNozzle?.Tank?.Fuel?.Id == fuel.Id)
            {
                SelectedNozzle.Tank.Fuel.Price = fuel.Price;
                CleanForm();
            }
        }

        private void FuelSaleService_OnUpdated(FuelSale fuelSale)
        {
            try
            {
                Nozzle? nozzle = Nozzles.FirstOrDefault(n => n.Id == fuelSale.NozzleId);

                //if (nozzle != null)
                //{
                //    nozzle.FuelSale = fuelSale;
                //}
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        private async Task<bool> CanFuelSale()
        {
            if (!ValidateNozzleSelection() || !ValidateShift() || !ValidateCashRegisterShift()
                || !await ValidateFuelQuantity() || !ValidatePayment())
            {
                return false;
            }

            return true;
        }

        private async Task<bool> CheckUncomplatedFuelSaleAsync(Nozzle selectedNozzle)
        {
            var fuelSale = await _fuelSaleService.GetUncompletedFuelSaleAsync(selectedNozzle.Id, _shiftStore.CurrentShift.Id);

            return fuelSale != null;
        }

        private bool ValidateShift()
        {
            if (_shiftStore.CurrentShift == null)
            {
                MessageBoxService.ShowMessage("Смена СУ не открыта. Откройте новую смену!", "Внимание", MessageButton.OK, MessageIcon.Exclamation);
                return false;
            }
            else
            {
                switch (_shiftStore.CurrentShiftState)
                {
                    case ShiftState.Closed:
                        MessageBoxService.ShowMessage("Смена СУ закрыта. Открыть новую смену!", "Внимание", MessageButton.OK, MessageIcon.Exclamation);
                        return false;
                    case ShiftState.Exceeded24Hours:
                        MessageBoxService.ShowMessage("Смена СУ работает более 24 часов. Закройте текущую смену и откройте новую!", "Внимание", MessageButton.OK, MessageIcon.Exclamation);
                        return false;
                    case ShiftState.Open:
                        return true;
                }
            }
            return true;
        }

        private bool ValidateCashRegisterShift()
        {
            // Проверяем, если тип оплаты не наличный или безналичный – пропускаем проверку ККМ
            if (CreateFuelSale.PaymentType is not PaymentType.Cash or PaymentType.Cashless)
            {
                return true;
            }

            //Проверяем, настроено ли ККМ в Конфигураторе оборудования
            if (_cashRegisterStore.CashRegister == null)
            {
                MessageBoxService.ShowMessage(
                        "Ошибка конфигурации!",
                        "ККМ не настроено. Проверьте настройки в Конфигураторе оборудования.",
                        MessageButton.OK,
                        MessageIcon.Error
                    );
                return false;
            }

            // Определяем сообщение в зависимости от текущего статуса ККМ
            string? message = _cashRegisterStore.Status switch
            {
                CashRegisterStatus.Exceeded24Hours => "Смена на ККМ открыта более 24 часов. Пожалуйста, закройте смену и откройте новую.",
                CashRegisterStatus.Close => "Смена на ККМ закрыта. Пожалуйста, откройте новую смену перед началом работы.",
                CashRegisterStatus.Unknown => "Статус ККМ неизвестен. Проверьте работу ККМ.",
                _ => null
            };

            // Если сообщение определено, выводим его пользователю
            if (message != null)
            {
                _ = MessageBoxService.ShowMessage(message, "Внимание!", MessageButton.OK, MessageIcon.Warning);
                return false;
            }
            return true;
        }

        private bool ValidateNozzleSelection()
        {
            if (SelectedNozzle == null)
            {
                MessageBoxService.ShowMessage("Выберите ТРК!", "Внимание", MessageButton.OK, MessageIcon.Exclamation);
                return false;
            }

            if (SelectedNozzle.Status != NozzleStatus.Ready)
            {
                MessageBoxService.ShowMessage($"{SelectedNozzle.Name} занята или заблокирована.", "Внимание", MessageButton.OK, MessageIcon.Exclamation);
                return false;
            }

            return true;
        }

        private async Task<bool> ValidateFuelQuantity()
        {
            if (Quantity <= 0)
            {
                MessageBoxService.ShowMessage("Введите количество топлива!", "Внимание", MessageButton.OK, MessageIcon.Exclamation);
                return false;
            }

            if (Quantity < 1)
            {
                MessageBoxService.ShowMessage("Минимальная доза отпуска 1 литр!", "Внимание!", MessageButton.OK, MessageIcon.Exclamation);
                return false;
            }

            IEnumerable<TankFuelQuantityView> tanks = await _tankFuelQuantityView.GetAllAsync();

            TankFuelQuantityView tank = tanks.FirstOrDefault(t => t.Id == SelectedNozzle.TankId);
            if (tank == null)
            {
                MessageBoxService.ShowMessage("Резервуар не найден!", "Внимание!", MessageButton.OK, MessageIcon.Exclamation);
                return false;
            }

            if (tank.MinimumSize > 0)
            {
                if ((tank.CurrentFuelQuantity - Quantity) <= tank.MinimumSize)
                {
                    MessageBoxService.ShowMessage("Недостаточно топлива в резервуаре с учетом мертвого остатка", "Внимание!", MessageButton.OK, MessageIcon.Exclamation);
                    return false;
                }
            }
            else
            {
                if ((tank.CurrentFuelQuantity - Quantity) < 0)
                {
                    MessageBoxService.ShowMessage("Недостаточно топлива в резервуаре!", "Внимание!", MessageButton.OK, MessageIcon.Exclamation);
                    return false;
                }
            }
            return true;
        }

        private bool ValidatePayment()
        {
            if (CreateFuelSale.PaymentType == PaymentType.None)
            {
                MessageBoxService.ShowMessage("Выберите вид оплаты!", "Внимание", MessageButton.OK, MessageIcon.Exclamation);
                return false;
            }
            return true;
        }

        private void CleanForm()
        {
            CreateFuelSale = new() { PaymentType = PaymentType.Cash };
            Quantity = 0;
            Tube = null;
            SelectedNozzle = null;
        }

        private void ShiftStore_OnNozzleSelectionChanged(Nozzle nozzle)
        {
            Tube = nozzle.Tube;
        }

        private void NozzleSelectionChanged()
        {
            _shiftStore.OnNozzleSelectionChanged -= ShiftStore_OnNozzleSelectionChanged;
            if (SelectedNozzle != null && SelectedNozzle.Tank != null && SelectedNozzle.Tank.Fuel != null)
            {
                CreateFuelSale.TankId = SelectedNozzle.Tank.Id;
                CreateFuelSale.Tank = SelectedNozzle.Tank;
                CreateFuelSale.Tank.Fuel = SelectedNozzle.Tank.Fuel;
                CreateFuelSale.Price = SelectedNozzle.Tank.Fuel.Price;
            }

            if (SelectedNozzle != null)
            {
                _shiftStore.NozzleSelectionChanged(SelectedNozzle);
            }
            _shiftStore.OnNozzleSelectionChanged += ShiftStore_OnNozzleSelectionChanged;
        }

        private void OnNozzleSelected(int tube)
        {
            if (Tube == null)
            {
                Tube = tube;
                SetFuelNozzle();
                return;
            }

            if (Tube.Value != tube)
            {
                Tube = tube;
                SetFuelNozzle();
            }
            else
            {
                FocusSumTextEdit();
            }
        }

        private void FocusSumTextEdit()
        {
            if (SelectedNozzle != null && _sumTextEdit != null)
            {
                _sumTextEdit.Dispatcher.BeginInvoke(new Action(() => _sumTextEdit.Focus()), DispatcherPriority.Background);
            }
        }

        #endregion

        #region Dispose

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _fuelService.OnUpdated -= FuelService_OnUpdated;
                _fuelSaleService.OnUpdated -= FuelSaleService_OnUpdated;
                _shiftStore.OnNozzleSelectionChanged -= ShiftStore_OnNozzleSelectionChanged;
                _nozzleStore.OnNozzleSelected -= OnNozzleSelected;
                _hotKeysService.OnHotKeyPressed -= HotKeysService_OnHotKeyPressed;
            }

            base.Dispose(disposing);
        }

        #endregion
    }
}
