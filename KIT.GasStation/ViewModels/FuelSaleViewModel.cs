using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Xpf.Editors;
using KIT.GasStation.Domain.Models;
using KIT.GasStation.Domain.Services;
using KIT.GasStation.Domain.Views;
using KIT.GasStation.Helpers;
using KIT.GasStation.Services;
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
        private readonly INozzleStore _nozzleStore;
        private readonly IViewService<TankFuelQuantityView> _tankFuelQuantityView;
        private readonly ILogger<FuelSaleViewModel> _logger;
        private readonly IFuelService _fuelService;
        private readonly IShiftStore _shiftStore;
        private readonly IDisсountStore _disсountStore;
        private readonly ICashRegisterStore _cashRegisterStore;
        private readonly IHotKeysService _hotKeysService;
        private int? _tube;
        private Nozzle? _selectedNozzle;
        private FuelSale _createFuelSale = new() { PaymentType = PaymentType.Cash };
        private bool _isUpdatingQuantity;
        private bool _isUpdatingSum;
        private bool _isSumUpdating;
        private decimal _sum;
        private decimal _quantity;
        private TextEdit _sumTextEdit;

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
                SetFuelNozzle();
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
                FocusSumTextEdit();
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
            IHotKeysService hotKeysService)
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

            Title = "Панель заявок";

            _fuelService.OnUpdated += FuelService_OnUpdated;
            _fuelSaleService.OnUpdated += FuelSaleService_OnUpdated;
            _fuelSaleService.OnCreated += FuelSaleService_OnCreated;
            _shiftStore.OnNozzleSelectionChanged += ShiftStore_OnNozzleSelectionChanged;
            _nozzleStore.OnNozzleSelected += OnNozzleSelected;
            _cashRegisterStore.OnReceiptPrinting += CashRegisterStore_OnReceiptPrinting;
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

        #endregion

        #region Hot Keys

        private async void HotKeysService_OnHotKeyPressed(HotKeyAction hotKeyAction)
        {
            switch (hotKeyAction)
            {
                case HotKeyAction.FuelSale:
                    await OpenPayView();
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
                    var fiscalData = _cashRegisterStore.SaleAsync(CreateFuelSale, SelectedNozzle.Tank.Fuel);

                    if (fiscalData != null)
                    {
                        CreateFuelSale.FiscalData = await fiscalData;
                        await _fuelSaleService.CreateAsync(CreateFuelSale);
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

            CreateFuelSale.TankId = SelectedNozzle.TankId;
            CreateFuelSale.ShiftId = _shiftStore.CurrentShift.Id;
            CreateFuelSale.NozzleId = SelectedNozzle.Id;
            CreateFuelSale.Sum = Sum;
            CreateFuelSale.Quantity = Quantity;
            CreateFuelSale.Price = SelectedNozzle.Tank.Fuel.Price;
            CreateFuelSale.DiscountSale = _disсountStore.CalculateDiscount(CreateFuelSale, SelectedNozzle.Tank.Fuel.Id, _isSumUpdating);
            CreateFuelSale.ReceivedCount = SelectedNozzle.LastCounter;
            CreateFuelSale.IsForSum = _isSumUpdating;

            PayViewModel viewModel = new(_fuelSaleService, _disсountStore, _cashRegisterStore)
            {
                CreateFuelSale = CreateFuelSale,
                SelectedNozzle = SelectedNozzle,
            };

            WindowService.Title = "Оплата";
            WindowService.Show(nameof(PayView), viewModel);
        }

        private void SetFuelNozzle()
        {
            if (Tube == null)
            {
                return;
            }

            Quantity = 0;

            SelectedNozzle = null;

            Nozzle? nozzle = Nozzles.FirstOrDefault(n => n.Tube == Tube);

            if (nozzle != null)
            {
                SelectedNozzle = nozzle;

                _nozzleStore.SelectNozzle(Tube.Value);
            }
        }

        private void CashRegisterStore_OnReceiptPrinting()
        {
            _ = _fuelSaleService.CreateAsync(CreateFuelSale);
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

                if (nozzle != null)
                {
                    nozzle.FuelSale = fuelSale;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        private void FuelSaleService_OnCreated(FuelSale fuelSale)
        {
            CleanForm();
        }

        private async Task<bool> CanFuelSale()
        {
            if (!ValidateNozzleSelection() || !await ValidateShift() || !ValidateCashRegisterShift()
                || !await ValidateFuelQuantity() || !ValidatePayment())
            {
                return false;
            }



            return true;
        }

        private async Task<bool> ValidateShift()
        {
            MessageResult result = MessageResult.None;
            if (_shiftStore.CurrentShift == null)
            {
                result = MessageBoxService.ShowMessage("Смена не открыта. Открыть новую смену?", "Внимание", MessageButton.YesNo, MessageIcon.Question);
            }
            else
            {
                switch (_shiftStore.CurrentShiftState)
                {
                    case ShiftState.Closed:
                        result = MessageBoxService.ShowMessage("Смена закрыта. Открыть новую смену?", "Внимание", MessageButton.YesNo, MessageIcon.Question);
                        break;
                    case ShiftState.Exceeded24Hours:
                        result = MessageBoxService.ShowMessage("Смена работает более 24 часов. Закрыть текущую смену и открыть новую?", "Внимание", MessageButton.YesNo, MessageIcon.Question);
                        if (result == MessageResult.Yes)
                        {
                            await _shiftStore.CloseShiftAsync();
                        }
                        break;
                }
            }
            if (result == MessageResult.Yes)
            {
                await _shiftStore.OpenShiftAsync();
                return false;
            }

            if (result == MessageResult.No)
            {
                return false;
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
            //string? message = _cashRegisterStore.CashRegister.Status switch
            //{
            //    CashRegisterStatus.Exceeded24Hours => "Смена на ККМ открыта более 24 часов. Пожалуйста, закройте смену и откройте новую.",
            //    CashRegisterStatus.Close => "Смена на ККМ закрыта. Пожалуйста, откройте новую смену перед началом работы.",
            //    CashRegisterStatus.Error => "Ошибка ККМ. Проверьте соединение с сервером или настройки кассы.",
            //    CashRegisterStatus.Unknown => "Статус ККМ неизвестен. Проверьте работу ККМ.",
            //    CashRegisterStatus.NoOpenedShift => "Смена на ККМ не открыта. Откройте смену перед началом работы.",
            //    _ => null
            //};

            //// Если сообщение определено, выводим его пользователю
            //if (message != null)
            //{
            //    _ = MessageBoxService.ShowMessage(message, "Внимание!", MessageButton.OK, MessageIcon.Warning);
            //    return false;
            //}
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
                _sumTextEdit.Dispatcher.BeginInvoke(new Action(() => _sumTextEdit.Focus()),
                    DispatcherPriority.Background);
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
                _cashRegisterStore.OnReceiptPrinting -= CashRegisterStore_OnReceiptPrinting;
                _hotKeysService.OnHotKeyPressed -= HotKeysService_OnHotKeyPressed;
            }

            base.Dispose(disposing);
        }

        #endregion
    }
}
