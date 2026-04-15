using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using KIT.GasStation.Domain.Models;
using KIT.GasStation.Domain.Models.CashRegisters;
using KIT.GasStation.Domain.Services;
using KIT.GasStation.Domain.Views;
using KIT.GasStation.FuelDispenser.Hubs;
using KIT.GasStation.FuelDispenser.Models;
using KIT.GasStation.Services;
using KIT.GasStation.State.CashRegisters;
using KIT.GasStation.State.Nozzles;
using KIT.GasStation.State.Shifts;
using KIT.GasStation.State.Users;
using KIT.GasStation.ViewModels.Base;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace KIT.GasStation.ViewModels
{
    /// <summary>
    /// ViewModel управления топливно-раздаточной колонкой (ТРК).
    /// Обеспечивает отображение пистолетов, взаимодействие с хабом SignalR
    /// и управление жизненным циклом продаж топлива.
    /// </summary>
    public partial class FuelDispenserViewModel : FuelDispenserBaseViewModel
    {
        #region Private Members

        private readonly INozzleStore _nozzleStore;
        private readonly IFuelSaleService _fuelSaleService;
        private readonly IFiscalDataService _fiscalDataService;
        private readonly IFuelService _fuelService;
        private readonly ICashRegisterStore _cashRegisterStore;
        private readonly IShiftStore _shiftStore;
        private readonly IShiftCounterService _shiftCounterService;
        private readonly IUnregisteredSaleService _unregisteredSaleService;
        private readonly IUserStore _userStore;
        private readonly IHubClient _hubClient;
        private readonly IHotKeysService _hotKeysService;
        private readonly IViewService<TankFuelQuantityView> _tankFuelQuantityView;
        private readonly INozzleService _nozzleService;
        private readonly ILogger<FuelDispenserViewModel> _logger;

        private HubConnection _hub;
        private int _side;
        private NozzleStatus _status;
        private ObservableCollection<Nozzle> _nozzle = new();
        private Nozzle _selectedNozzle;
        private int _connectionLostHandled = 0;
        private readonly SemaphoreSlim _countersGate = new(1, 1);
        private CancellationTokenSource _cts;
        private Task _startTask;
        private bool _hubHandlersRegistered;
        private int _hubReconnectLoop;
        private decimal _receivedSum;
        private decimal _receivedQuantity;
        private readonly List<IDisposable> _hubSubscriptions = new();

        private const string WorkerOfflineDueToHubMessage = "Нет связи с сервером";

        #endregion

        #region Public Properties

        /// <summary>Номер стороны (1 = левая, 2 = правая) ТРК.</summary>
        public int Side
        {
            get => _side;
            set
            {
                _side = value;
                OnPropertyChanged(nameof(Side));
                OnPropertyChanged(nameof(Caption));
            }
        }

        /// <summary>Заголовок стороны для отображения на панели.</summary>
        public string Caption => $"- {Side} -";

        /// <summary>Текущий агрегированный статус пистолета на данной стороне.</summary>
        public NozzleStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        /// <summary>Коллекция пистолетов данной стороны ТРК.</summary>
        public ObservableCollection<Nozzle> Nozzles
        {
            get => _nozzle;
            set
            {
                _nozzle = value;
                OnPropertyChanged(nameof(Nozzles));
            }
        }

        /// <summary>Текущий выбранный пистолет.</summary>
        public Nozzle SelectedNozzle
        {
            get => _selectedNozzle;
            set
            {
                _selectedNozzle = value;
                OnPropertyChanged(nameof(SelectedNozzle));
            }
        }

        /// <summary>
        /// Фактически полученная сумма за текущую заправку.
        /// При незавершённой продаже добавляется к базовой сумме возобновления.
        /// </summary>
        public decimal ReceivedSum
        {
            get => _receivedSum;
            set
            {
                _receivedSum = value;
                OnPropertyChanged(nameof(ReceivedSum));

                if (SelectedNozzle?.CurrentFuelSale is null) return;

                if (SelectedNozzle.CurrentFuelSale.FuelSaleStatus is FuelSaleStatus.Uncompleted)
                    SelectedNozzle.CurrentFuelSale.ReceivedSum = SelectedNozzle.CurrentFuelSale.ResumeBaseSum + value;
                else
                    SelectedNozzle.CurrentFuelSale.ReceivedSum = value;
            }
        }

        /// <summary>
        /// Фактически отпущенный объём топлива за текущую заправку (в литрах).
        /// При незавершённой продаже добавляется к базовому объёму возобновления.
        /// </summary>
        public decimal ReceivedQuantity
        {
            get => _receivedQuantity;
            set
            {
                _receivedQuantity = value;
                OnPropertyChanged(nameof(ReceivedQuantity));

                if (SelectedNozzle?.CurrentFuelSale is null) return;

                if (SelectedNozzle.CurrentFuelSale.FuelSaleStatus is FuelSaleStatus.Uncompleted)
                    SelectedNozzle.CurrentFuelSale.ReceivedQuantity = SelectedNozzle.CurrentFuelSale.ResumeBaseQuantity + value;
                else
                    SelectedNozzle.CurrentFuelSale.ReceivedQuantity = value;
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Инициализирует ViewModel и подписывается на события всех сервисов.
        /// </summary>
        public FuelDispenserViewModel(
            INozzleStore nozzleStore,
            IFuelSaleService fuelSaleService,
            ICashRegisterStore cashRegisterStore,
            IShiftStore shiftStore,
            IShiftCounterService shiftCounterService,
            IUnregisteredSaleService unregisteredSaleService,
            IFuelService fuelService,
            IUserStore userStore,
            IHubClient hubClient,
            IViewService<TankFuelQuantityView> tankFuelQuantityView,
            IHotKeysService hotKeysService,
            INozzleService nozzleService,
            IFiscalDataService fiscalDataService,
            ILogger<FuelDispenserViewModel> logger)
        {
            _nozzleStore = nozzleStore;
            _fuelSaleService = fuelSaleService;
            _cashRegisterStore = cashRegisterStore;
            _shiftStore = shiftStore;
            _shiftCounterService = shiftCounterService;
            _unregisteredSaleService = unregisteredSaleService;
            _fuelService = fuelService;
            _userStore = userStore;
            _hubClient = hubClient;
            _tankFuelQuantityView = tankFuelQuantityView;
            _hotKeysService = hotKeysService;
            _nozzleService = nozzleService;
            _fiscalDataService = fiscalDataService;
            _logger = logger;

            _shiftStore.OnLogin += ShiftStore_OnLogin;
            _shiftStore.OnOpened += ShiftStore_OnOpened;
            _shiftStore.OnClosed += ShiftStore_OnClosed;
            _fuelSaleService.OnCreated += FuelSaleService_OnCreated;
            _fuelSaleService.OnUpdated += FuelSaleService_OnUpdated;
            _fuelSaleService.OnResumeFueling += FuelSaleService_OnResumeFueling;
            _nozzleStore.OnNozzleSelected += OnNozzleSelected;
            _nozzleStore.OnNozzleCountersRequested += OnNozzleCountersRequested;
            _fuelService.OnUpdated += FuelService_OnUpdated;
            _userStore.OnLogout += UserStore_OnLogout;
            _hotKeysService.OnNumberKeyPressed += HotKeysService_OnNumberKeyPressed;
            _nozzleService.OnCreated += NozzleService_OnCreated;
            _nozzleService.OnUpdated += NozzleService_OnUpdated;
            _nozzleService.OnDeleted += NozzleService_OnDeleted;
            _fiscalDataService.OnCreated += FiscalDataService_OnCreated;

            _logger.LogInformation("Создание FuelDispenserViewModel для стороны {Side}", Side);
        }

        #endregion

        #region Commands

        /// <summary>
        /// Команда возобновления ранее прерванной заправки.
        /// Отправляет оставшуюся сумму к отпуску на хаб.
        /// </summary>
        [Command]
        public async Task ResumeFueling()
        {
            try
            {
                if (SelectedNozzle is null)
                {
                    _logger.LogWarning("Попытка возобновить заправку без выбранного пистолета");
                    return;
                }

                if (_hub?.State != HubConnectionState.Connected)
                {
                    _logger.LogError("HubConnection не активен (состояние: {State})", _hub?.State);
                    return;
                }

                var resumeFuelingRequest = new ResumeFuelingRequest
                {
                    GroupName = SelectedNozzle.Group,
                    Quantity = SelectedNozzle.CurrentFuelSale.Quantity - SelectedNozzle.CurrentFuelSale.ReceivedQuantity,
                    Sum = SelectedNozzle.CurrentFuelSale.Sum - SelectedNozzle.CurrentFuelSale.ReceivedSum
                };

                await _hub.InvokeAsync("ResumeFuelingAsync", resumeFuelingRequest);

                _logger.LogInformation("Команда ResumeFueling отправлена: группа={Group}", SelectedNozzle.Group);
            }
            catch (HubException ex)
            {
                _logger.LogError(ex, "HubException при возобновлении заправки: {Message}", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при возобновлении заправки пистолета {Group}", SelectedNozzle?.Group);
            }
        }

        /// <summary>
        /// Обработчик нажатия мыши на элементе списка пистолетов.
        /// Запрещает переключение пистолета во время активной заправки.
        /// </summary>
        [Command]
        public void NozzleMouseDown(MouseButtonEventArgs args)
        {
            try
            {
                var selectionAllowed = Status switch
                {
                    NozzleStatus.PumpWorking => false,
                    NozzleStatus.WaitingStop => false,
                    NozzleStatus.WaitingRemoved => false,
                    _ => true
                };

                if (!selectionAllowed)
                {
                    args.Handled = true;
                    return;
                }

                var item = FindListBoxItemFromEvent(args);
                if (item?.Content is Nozzle clickedNozzle)
                    _nozzleStore.SelectNozzle(clickedNozzle.Tube);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при выборе пистолета мышью");
            }
        }

        /// <summary>
        /// Команда завершения заправки — сброс показаний колонки.
        /// </summary>
        [Command]
        public async Task CompleteFueling()
        {
            try
            {
                await _hub.InvokeAsync("CompleteFuelingAsync", SelectedNozzle.Group);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при завершении заправки (группа={Group})", SelectedNozzle?.Group);
            }
        }

        /// <summary>
        /// Команда аварийной остановки заправки.
        /// </summary>
        [Command]
        public async Task StopFueling()
        {
            try
            {
                await _hub.InvokeAsync("StopFuelingAsync", SelectedNozzle.Group);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при остановке заправки (группа={Group})", SelectedNozzle?.Group);
            }
        }

        /// <summary>
        /// Команда переключения режима управления ТРК (программный ↔ клавиатурный).
        /// </summary>
        [Command]
        public async Task ChangeControlMode()
        {
            if (SelectedNozzle is null) return;

            try
            {
                await _hub.InvokeAsync("ChangeControlModeAsync", SelectedNozzle.Group, true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при смене режима управления ТРК (группа={Group})", SelectedNozzle.Group);
            }
        }

        /// <summary>
        /// Команда запуска заправки «полный бак» на фиксированный объём из настроек.
        /// </summary>
        [Command]
        public async Task StartFullFueling()
        {
            try
            {
                if (SelectedNozzle is null) return;

                var limitLiters = Properties.Settings.Default.LimitLitersFullFueling;

                FuelSale fuelSale = new()
                {
                    TankId = SelectedNozzle.TankId,
                    ShiftId = _shiftStore.CurrentShift.Id,
                    NozzleId = SelectedNozzle.Id,
                    Sum = limitLiters * SelectedNozzle.Tank.Fuel.Price,
                    Quantity = limitLiters,
                    Price = SelectedNozzle.Tank.Fuel.Price,
                    ReceivedCount = SelectedNozzle.LastCounter,
                    IsForSum = false,
                    CreateDate = DateTime.Now,
                    PaymentType = PaymentType.Cash,
                };

                FiscalData newFiscalData = new()
                {
                    OperationType = OperationType.Sale,
                    PaymentType = fuelSale.PaymentType,
                    Price = fuelSale.Price,
                    Quantity = fuelSale.Quantity,
                    Total = fuelSale.Sum,
                    UnitOfMeasurement = SelectedNozzle.Tank.Fuel.UnitOfMeasurement.Name,
                    FuelName = SelectedNozzle.Tank.Fuel.Name,
                    ValueAddedTax = SelectedNozzle.Tank.Fuel.ValueAddedTax,
                    SalesTax = SelectedNozzle.Tank.Fuel.SalesTax,
                    Tnved = SelectedNozzle.Tank.Fuel.TNVED,
                };

                if (Properties.Settings.Default.ReceiptPrintingMode == "Before")
                {
                    var fiscalData = await _cashRegisterStore.SaleAsync(newFiscalData);
                    if (fiscalData is not null)
                    {
                        await _fuelSaleService.CreateAsync(fuelSale);
                        fiscalData.FuelSaleId = fuelSale.Id;
                        await _fiscalDataService.CreateAsync(fiscalData);
                    }
                    else
                    {
                        MessageBoxService.ShowMessage(
                            "Не удалось получить фискальные данные от ККМ.", "Ошибка",
                            MessageButton.OK, MessageIcon.Error);
                    }
                }
                else
                {
                    await _fuelSaleService.CreateAsync(fuelSale);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при запуске заправки полный бак");
                MessageBoxService.ShowMessage(
                    "Ошибка при создании продажи: " + ex.Message, "Ошибка",
                    MessageButton.OK, MessageIcon.Error);
            }
        }

        #endregion

        #region Lifecycle

        /// <summary>
        /// Запускает подключение к SignalR и загружает начальные данные пистолетов.
        /// Вызывается при авторизации пользователя.
        /// </summary>
        private async Task StartAsync(CancellationToken token)
        {
            try
            {
                if (Nozzles.Count == 0) return;

                await GetNozzleLastFuelSale();

                _hub = _hubClient.Connection;
                RegisterHubHandlers();

                await _hubClient.EnsureStartedAsync(token);
                await JoinAndSubscribeAsync();
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("StartAsync отменён для стороны {Side}", Side);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка в StartAsync для стороны {Side}", Side);
            }
        }

        /// <summary>
        /// Останавливает работу ViewModel: покидает SignalR-группы, отписывается от всех событий.
        /// </summary>
        public async Task StopAsync()
        {
            try
            {
                if (_hub is not null)
                {
                    foreach (var item in Nozzles)
                        await _hub.InvokeAsync("LeaveController", item.Group);
                }

                // Отписка от событий сервисов
                _shiftStore.OnLogin -= ShiftStore_OnLogin;
                _shiftStore.OnOpened -= ShiftStore_OnOpened;
                _shiftStore.OnClosed -= ShiftStore_OnClosed;
                _fuelSaleService.OnCreated -= FuelSaleService_OnCreated;
                _fuelSaleService.OnUpdated -= FuelSaleService_OnUpdated;
                _fuelSaleService.OnResumeFueling -= FuelSaleService_OnResumeFueling;
                _nozzleStore.OnNozzleSelected -= OnNozzleSelected;
                _nozzleStore.OnNozzleCountersRequested -= OnNozzleCountersRequested;
                _fuelService.OnUpdated -= FuelService_OnUpdated;
                _userStore.OnLogout -= UserStore_OnLogout;
                _hotKeysService.OnNumberKeyPressed -= HotKeysService_OnNumberKeyPressed;
                _nozzleService.OnCreated -= NozzleService_OnCreated;
                _nozzleService.OnUpdated -= NozzleService_OnUpdated;
                _nozzleService.OnDeleted -= NozzleService_OnDeleted;
                _fiscalDataService.OnCreated -= FiscalDataService_OnCreated;

                UnregisterHubHandlers();

                _cts?.Cancel();

                if (_startTask is not null)
                    await _startTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при остановке FuelDispenserViewModel стороны {Side}", Side);
            }
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// Определяет, завершена ли заправка: фактический объём ≥ запрошенному с допуском 0,001 л.
        /// </summary>
        private static bool IsFuelingCompleted(FuelSale sale)
        {
            const decimal eps = 0.0005m;
            return sale.ReceivedQuantity + eps >= sale.Quantity;
        }

        /// <summary>
        /// Ищет элемент <see cref="ListBoxItem"/> в визуальном дереве по событию мыши.
        /// </summary>
        private static ListBoxItem? FindListBoxItemFromEvent(MouseButtonEventArgs e)
        {
            DependencyObject current = e.OriginalSource as DependencyObject;
            while (current is not null and not ListBoxItem)
                current = VisualTreeHelper.GetParent(current);
            return current as ListBoxItem;
        }

        #endregion
    }
}
