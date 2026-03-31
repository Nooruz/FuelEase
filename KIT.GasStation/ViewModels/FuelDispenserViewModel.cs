using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using KIT.GasStation.Domain.Models;
using KIT.GasStation.Domain.Services;
using KIT.GasStation.Domain.Views;
using KIT.GasStation.FuelDispenser.Commands;
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace KIT.GasStation.ViewModels
{
    public class FuelDispenserViewModel : FuelDispenserBaseViewModel
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
        private const string WorkerOfflineDueToHubMessage = "Нет связи с сервером";
        private decimal _receivedSum;
        private decimal _receivedQuantity;
        private readonly List<IDisposable> _hubSubscriptions = new();

        #endregion

        #region Public Properties

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
        public string Caption => $"- {Side} -";
        public NozzleStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }
        public ObservableCollection<Nozzle> Nozzles
        {
            get => _nozzle;
            set
            {
                _nozzle = value;
                OnPropertyChanged(nameof(Nozzles));
            }
        }
        public Nozzle SelectedNozzle
        {
            get => _selectedNozzle;
            set
            {
                _selectedNozzle = value;
                OnPropertyChanged(nameof(SelectedNozzle));
            }
        }
        public decimal ReceivedSum
        {
            get => _receivedSum;
            set
            {
                _receivedSum = value;
                OnPropertyChanged(nameof(ReceivedSum));

                if (SelectedNozzle.CurrentFuelSale.FuelSaleStatus is FuelSaleStatus.Uncompleted)
                {
                    SelectedNozzle.CurrentFuelSale.ReceivedSum = SelectedNozzle.CurrentFuelSale.ResumeBaseSum + value;
                }
                else
                {
                    SelectedNozzle.CurrentFuelSale.ReceivedSum = value;
                }
            }
        }
        public decimal ReceivedQuantity
        {
            get => _receivedQuantity;
            set
            {
                _receivedQuantity = value;
                OnPropertyChanged(nameof(ReceivedQuantity));

                if (SelectedNozzle.CurrentFuelSale.FuelSaleStatus is FuelSaleStatus.Uncompleted)
                {
                    SelectedNozzle.CurrentFuelSale.ReceivedQuantity = SelectedNozzle.CurrentFuelSale.ResumeBaseQuantity + value;
                }
                else
                {
                    SelectedNozzle.CurrentFuelSale.ReceivedQuantity = value;
                }
            }
        }

        //public FuelSale CurrentFuelSale
        //{
        //    get => _currentFuelSale;
        //    set
        //    {
        //        _currentFuelSale = value;
        //        OnPropertyChanged(nameof(CurrentFuelSale));
        //    }
        //}

        #endregion

        #region Constructors

        public FuelDispenserViewModel(INozzleStore nozzleStore,
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
            _logger = logger;
            _fiscalDataService = fiscalDataService;

            _shiftStore.OnLogin += ShiftStore_OnLogin;
            _fuelSaleService.OnCreated += FuelSaleService_OnCreated;
            _fuelSaleService.OnUpdated += FuelSaleService_OnUpdated;
            _fuelSaleService.OnResumeFueling += FuelSaleService_OnResumeFueling;
            _nozzleStore.OnNozzleSelected += OnNozzleSelected;
            _nozzleStore.OnNozzleCountersRequested += OnNozzleCountersRequested;
            _shiftStore.OnOpened += ShiftStore_OnOpened;
            _shiftStore.OnClosed += ShiftStore_OnClosed;
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

        #region Public Voids

        /// <summary>
        /// Команда продолжения заправки
        /// </summary>
        /// <returns></returns>
        [Command]
        public async Task ResumeFueling()
        {
            try
            {
                if (SelectedNozzle == null)
                {
                    _logger.LogWarning("Попытка продолжить заправку без выбранного пистолета");
                    return;
                }

                if (_hub?.State != HubConnectionState.Connected)
                {
                    _logger.LogError("HubConnection не активен. Состояние: {State}", _hub?.State);
                    return;
                }

                _logger.LogDebug("Вызов ResumeFueling для пистолета {NozzleGroup}", SelectedNozzle?.Group ?? "null");

                var resumeFuelingRequest = new ResumeFuelingRequest
                {
                    GroupName = SelectedNozzle?.Group ?? "null",
                    Value = SelectedNozzle.CurrentFuelSale?.Sum - SelectedNozzle.CurrentFuelSale?.ReceivedSum ?? 0m
                };

                await _hub.InvokeAsync("ResumeFuelingAsync", resumeFuelingRequest);

                _logger.LogInformation("Команда ResumeFueling отправлена для группы {Group}", SelectedNozzle?.Group);
            }
            catch (HubException ex)
            {
                _logger.LogError(ex, "HubException при вызове продолжить: {Message}", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при вызове продолжить для пистолета {NozzleGroup}", SelectedNozzle?.Group ?? "null");
            }
        }

        [Command]
        public void NozzleMouseDoubleClick()
        {
            //if (SelectedNozzle == null)
            //{
            //    return;
            //}
            //_nozzleStore.SelectNozzle(SelectedNozzle.Tube);
        }

        [Command]
        public void NozzleMouseDown(MouseButtonEventArgs args)
        {
            try
            {
                var isSelectionAllowed = Status switch
                {
                    NozzleStatus.PumpWorking => false,
                    NozzleStatus.WaitingStop => false,
                    NozzleStatus.WaitingRemoved => false,
                    _ => true
                };

                if (!isSelectionAllowed)
                {
                    args.Handled = true;
                    return;
                }

                var item = FindListBoxItemFromEvent(args);
                if (item != null)
                {
                    var clickedItem = (Nozzle)item.Content;

                    // Это сработает даже если клик по уже выбранному элементу
                    _nozzleStore.SelectNozzle(clickedItem.Tube);
                }
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// Команда завершения заправки
        /// </summary>
        /// <returns></returns>
        [Command]
        public async Task CompleteFueling()
        {
            try
            {
                await _hub.InvokeAsync("CompleteFuelingAsync", SelectedNozzle.Group);
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// Команда остановки заправки
        /// </summary>
        [Command]
        public async Task StopFueling()
        {
            await _hub.InvokeAsync("StopFuelingAsync", SelectedNozzle.Group);
        }

        [Command]
        public async Task ChangeControlMode()
        {
            if (SelectedNozzle != null)
            {
                await ChangeControlModeAsync(SelectedNozzle);
            }
        }

        [Command]
        public async Task StartFullFueling()
        {
            try
            {
                if (SelectedNozzle != null)
                {
                    FuelSale fuelSale = new()
                    {
                        TankId = SelectedNozzle.TankId,
                        ShiftId = _shiftStore.CurrentShift.Id,
                        NozzleId = SelectedNozzle.Id,
                        Sum = Properties.Settings.Default.LimitLitersFullFueling * SelectedNozzle.Tank.Fuel.Price,
                        Quantity = Properties.Settings.Default.LimitLitersFullFueling,
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
                    };


                    if (Properties.Settings.Default.ReceiptPrintingMode == "Before")
                    {
                        var fiscalData = await _cashRegisterStore.SaleAsync(newFiscalData);

                        if (fiscalData != null)
                        {
                            await _fuelSaleService.CreateAsync(fuelSale);
                            fiscalData.FuelSaleId = fuelSale.Id;
                            await _fiscalDataService.CreateAsync(fiscalData);
                        }
                        else
                        {
                            MessageBoxService.ShowMessage("Не удалось получить фискальные данные от ККМ.", "Ошибка", MessageButton.OK, MessageIcon.Error);
                        }
                    }
                    else
                    {
                        await _fuelSaleService.CreateAsync(fuelSale);
                    }
                }
            }
            catch (Exception e)
            {
                MessageBoxService.ShowMessage("Ошибка при создании продажи: " + e.Message, "Ошибка", MessageButton.OK, MessageIcon.Error);
            }
        }

        #endregion

        #region ТРК Команды

        private async Task StartAsync(CancellationToken token)
        {
            try
            {
                if (Nozzles.Count == 0) return;

                // Получаем информацию о последних продажах по пистолетам
                await GetNozzleLastFuelSale();

                _hub = _hubClient.Connection;

                RegisterHubHandlers();

                await _hubClient.EnsureStartedAsync(token);

                await JoinAndSubscribeAsync();
            }
            catch (OperationCanceledException)
            {
                // Ожидаемая отмена, можно проигнорировать
            }
            catch (Exception)
            {

            }
        }

        public async Task StopAsync()
        {
            try
            {

                foreach (var item in Nozzles)
                {
                    await _hub.InvokeAsync("LeaveController", item.Group);
                }

                _shiftStore.OnLogin -= ShiftStore_OnLogin;
                _fuelSaleService.OnCreated -= FuelSaleService_OnCreated;
                _fuelSaleService.OnUpdated -= FuelSaleService_OnUpdated;
                _nozzleStore.OnNozzleSelected -= OnNozzleSelected;
                _nozzleStore.OnNozzleCountersRequested -= OnNozzleCountersRequested;
                _shiftStore.OnOpened -= ShiftStore_OnOpened;
                _shiftStore.OnClosed -= ShiftStore_OnClosed;
                _fuelService.OnUpdated -= FuelService_OnUpdated;
                _userStore.OnLogout -= UserStore_OnLogout;

                UnregisterHubHandlers();

                _cts?.Cancel();

                if (_startTask != null)
                {
                    await _startTask; // Ждём завершения StartAsync
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
            }
        }

        private async Task ChangeControlModeAsync(Nozzle nozzle)
        {
            await _hub.InvokeAsync("ChangeControlModeAsync", nozzle.Group, true);
        }

        /// <summary>
        /// Обработчик события завершения заправки
        /// </summary>
        private async Task OnCompletedFueling(Nozzle nozzle)
        {
            //if (response.Address == response.StatusAddress)
            //{
            //    _logger.LogDebug("OnCompletedFueling для пистолета {NozzleId}, статус: {Status}", nozzle.Id, response.Status);

            //    var fuelSale = SelectedNozzle?.FuelSale;

            //    if (fuelSale == null)
            //    {
            //        _logger.LogWarning("Нет активной продажи для завершения заправки на пистолете {NozzleId}",
            //            nozzle.Id);
            //        return;
            //    }

            //    try
            //    {
            //        decimal quantity = response.Quantity;
            //        decimal sum = response.Sum;

            //        ReceivedQuantity = quantity;
            //        ReceivedSum = sum;

            //        if (Properties.Settings.Default.ReceiptPrintingMode == "After")
            //        {
            //            if (fuelSale.FiscalData == null)
            //            {
            //                var fiscalData = await _cashRegisterStore.SaleAsync(fuelSale, fuelSale.Tank.Fuel, false);

            //                if (fiscalData is not null)
            //                {
            //                    fuelSale.FiscalData ??= fiscalData;
            //                }
            //                else
            //                {
            //                    MessageBoxService.ShowMessage("Не удалось получить фискальные данные от ККМ.", "Ошибка", MessageButton.OK, MessageIcon.Error);
            //                }
            //            }
            //        }
            //        else
            //        {
            //            if (fuelSale.Quantity > fuelSale.ReceivedQuantity)
            //            {
            //                if (fuelSale.PaymentType is PaymentType.Cash or PaymentType.Cashless)
            //                {
            //                    if (fuelSale.FiscalData?.ReturnCheck == null)
            //                    {
            //                        var returntFiscalData = await _cashRegisterStore
            //                        .ReturnAndReceivedSaleAsync(SelectedNozzle.FuelSale, SelectedNozzle.Tank.Fuel, _userStore.CurrentUser.FullName);

            //                        if (returntFiscalData != null)
            //                        {
            //                            fuelSale.FiscalData.ReturnCheck ??= returntFiscalData.Check;
            //                        }
            //                    }  
            //                }
            //            }
            //        }

            //        if (fuelSale.FuelSaleStatus is not FuelSaleStatus.Completed)
            //        {
            //            fuelSale.FuelSaleStatus = FuelSaleStatus.Completed;
            //            await _fuelSaleService.UpdateAsync(fuelSale.Id, fuelSale);
            //        }

            //        _logger.LogInformation("Продажа {SaleId} завершена успешно", fuelSale.Id);

            //        await _hub.InvokeAsync("GetCounterAsync", nozzle.Group);
            //    }
            //    catch (Exception ex)
            //    {
            //        _logger.LogError(ex, "Ошибка при завершении заправки для продажи {SaleId}", fuelSale.Id);
            //    }
            //}
        }

        /// <summary>
        /// Обработчик события потери соединения
        /// </summary>
        private void OnConnectionLost()
        {
            // Если _connectionLostHandled равен 0, то устанавливаем его в 1 и продолжаем обработку.
            // Если уже равен 1, значит обработка уже запущена – выходим.
            if (Interlocked.CompareExchange(ref _connectionLostHandled, 1, 0) != 0)
                return;

            try
            {
                if (Nozzles != null && Nozzles.Count > 0)
                {
                    foreach (Nozzle nozzle in Nozzles)
                    {
                        nozzle.Status = NozzleStatus.Unknown;
                    }
                }
            }
            finally
            {
                // Сбрасываем флаг через заданный интервал (например, 5000 мс)
                Task.Delay(5000).ContinueWith(_ =>
                {
                    Interlocked.Exchange(ref _connectionLostHandled, 0);
                });
            }
        }

        /// <summary>
        /// Обработчик события ожидания снятия пистолета
        /// </summary>
        private async Task OnWaitingAsync(string groupName)
        {
            var nozzle = Nozzles.FirstOrDefault(n => n.Group == groupName);

            if (nozzle == null) return;

            SelectedNozzle.CurrentFuelSale.FuelSaleStatus = FuelSaleStatus.Uncompleted;

            SelectedNozzle.CurrentFuelSale.ResumeBaseQuantity = SelectedNozzle.CurrentFuelSale.ReceivedQuantity;
            SelectedNozzle.CurrentFuelSale.ResumeBaseSum = SelectedNozzle.CurrentFuelSale.ReceivedSum;

            await _fuelSaleService.UpdateAsync(SelectedNozzle.CurrentFuelSale.Id, SelectedNozzle.CurrentFuelSale);
        }

        private async Task OnPumpStopAsync(FuelingResponse response)
        {
            var nozzle = Nozzles.FirstOrDefault(n => n.Group == response.GroupName);

            if (nozzle == null) return;

            // если оба нули — пропускаем
            if (response.Quantity == 0m && response.Sum == 0m)
                return;

            // если количество есть, а сумма = 0 → считаем сумму
            if (response.Quantity != 0m && response.Sum == 0m)
            {
                response.Sum = response.Quantity * nozzle.Price;
            }
            // если сумма есть, а количество = 0 → считаем количество
            else if (response.Sum != 0m && response.Quantity == 0m)
            {
                // защита от деления на ноль
                if (nozzle.Price <= 0m)
                    return;

                response.Quantity = response.Sum / nozzle.Price;
            }

            ReceivedQuantity = response.Quantity;
            ReceivedSum = response.Sum;

            await _hub.InvokeAsync("CompleteFuelingAsync", nozzle.Group);
        }

        /// <summary>
        /// Обработчик события ожидания остановки заправки
        /// </summary>
        private async Task OnWaitingStop(Nozzle nozzle)
        {
            //if (nozzle.FuelSale == null) return;

            //nozzle.FuelSale.FuelSaleStatus = FuelSaleStatus.Processed;
            //await _fuelSaleService.UpdateAsync(nozzle.FuelSale.Id, nozzle.FuelSale);
        }

        /// <summary>
        /// Обработчик события заполнения определенного объема
        /// </summary>
        private void OnFuelingAsync(FuelingResponse response)
        {
            var nozzle = Nozzles.FirstOrDefault(n => n.Group == response.GroupName);

            if (nozzle == null) return;

            if (nozzle.Status != NozzleStatus.PumpWorking)
            {
                nozzle.Status = NozzleStatus.PumpWorking;
            }

            // если оба нули — пропускаем
            if (response.Quantity == 0m && response.Sum == 0m)
                return;

            // если количество есть, а сумма = 0 → считаем сумму
            if (response.Quantity != 0m && response.Sum == 0m)
            {
                response.Sum = response.Quantity * nozzle.Price;
            }
            // если сумма есть, а количество = 0 → считаем количество
            else if (response.Sum != 0m && response.Quantity == 0m)
            {
                // защита от деления на ноль
                if (nozzle.Price <= 0m)
                    return;

                response.Quantity = response.Sum / nozzle.Price;
            }

            ReceivedQuantity = response.Quantity;
            ReceivedSum = response.Sum;
        }

        /// <summary>
        /// Обработчик события изменения статуса колонки
        /// </summary>ControllerResponse deviceResponse
        private void OnStatusChanged(StatusResponse response)
        {
            var currentNozzle = Nozzles.FirstOrDefault(n => n.Group == response.GroupName);
            if (currentNozzle is null) return;

            // Новый статус — сразу фиксируем, чтобы не работать со старым значением
            var newStatus = response.Status;
            Status = newStatus;

            // "Защищённая" колонка: если текущая продажа не завершена — её не трогать, держать PumpStop
            Nozzle? protectedNozzle = null;

            if (SelectedNozzle.CurrentFuelSale != null)
            {
                var hasUncompletedSale = SelectedNozzle.CurrentFuelSale.FuelSaleStatus is FuelSaleStatus.Uncompleted;

                if (hasUncompletedSale)
                {
                    protectedNozzle = SelectedNozzle;

                    var saleCompleted = IsFuelingCompleted(SelectedNozzle.CurrentFuelSale!);
                    if (!saleCompleted && protectedNozzle is not null)
                    {
                        // Продажа не завершена → фиксируем PumpStop и НЕ ДАЁМ её сбросить на Ready ниже
                        protectedNozzle.Status = NozzleStatus.PumpStop;
                        SelectedNozzle.CurrentFuelSale.ResumeBaseQuantity = SelectedNozzle.CurrentFuelSale.ReceivedQuantity;
                        SelectedNozzle.CurrentFuelSale.ResumeBaseSum = SelectedNozzle.CurrentFuelSale.ReceivedSum;
                    }
                }
            }

            // Если статус неизвестен — ничего больше не трогаем
            if (newStatus == NozzleStatus.Unknown)
                return;

            // Ставим статус текущей колонке
            // (Если это та же самая "protectedNozzle" и продажа не завершена — PumpStop важнее)
            if (protectedNozzle is null || protectedNozzle.Id != currentNozzle.Id || SelectedNozzle.CurrentFuelSale != null ||
                IsFuelingCompleted(SelectedNozzle.CurrentFuelSale!))
                currentNozzle.Status = newStatus;

            // Все остальные → Ready, кроме currentNozzle и protectedNozzle
            foreach (var n in Nozzles)
            {
                if (n.Id == currentNozzle.Id) continue;
                if (protectedNozzle is not null && n.Id == protectedNozzle.Id) continue;

                n.Status = NozzleStatus.Ready;
            }

            // Выбор сопла
            if (newStatus != NozzleStatus.Ready)
                SelectedNozzle = currentNozzle;
        }

        /// <summary>
        /// Обработчик события получения счетчика
        /// </summary>
        private async Task OnCounterReceived(FuelingResponse response)
        {
            var nozzle = Nozzles.FirstOrDefault(n => n.Group == response.GroupName);

            if (nozzle is null) return;

            nozzle.LastCounter = response.Quantity;

            var shift = _shiftStore.CurrentShift;
            if (shift == null)
                return;

            if (_shiftStore.CurrentShiftState is ShiftState.Closed)
                return;

            await CheckUncompletedSales();

            var shiftCounter = await _shiftCounterService.GetAsync(nozzle.Id, shift.Id);
            var totalSales = await _fuelSaleService.GetReceivedQuantityAsync(nozzle.Id, shift.Id);
            var unregisteredSales = await _unregisteredSaleService.GetAllAsync(nozzle.Id, shift.Id);

            if (shiftCounter == null)
                return;

            // Расчёт отклонения от начальных показаний
            decimal unregisteredSalesQuantity = unregisteredSales != null ? unregisteredSales.Sum(u => u.Quantity) : 0;
            decimal expectedCounter = shiftCounter.BeginSaleCounter + totalSales + unregisteredSalesQuantity;
            decimal unregisteredQuantity = nozzle.LastCounter - (shiftCounter.BeginNozzleCounter + expectedCounter);

            if (unregisteredQuantity != 0)
            {
                var unregisteredSale = new UnregisteredSale
                {
                    NozzleId = nozzle.Id,
                    ShiftId = shift.Id,
                    CreateDate = DateTime.Now,
                    State = UnregisteredSaleState.Waiting,
                    Quantity = unregisteredQuantity,
                    Sum = unregisteredQuantity * nozzle.Price
                };

                await _unregisteredSaleService.CreateAsync(unregisteredSale);
            }
        }

        private async Task OnCountersUpdated(List<CounterData> counterDatas)
        {
            await _countersGate.WaitAsync();

            try
            {
                foreach (var item in counterDatas)
                {
                    var nozzle = Nozzles.FirstOrDefault(n => n.Group == item.GroupName);

                    if (nozzle is null) continue;

                    nozzle.LastCounter = item.Counter;

                    var shift = _shiftStore.CurrentShift;
                    if (shift == null)
                        continue;

                    if (_shiftStore.CurrentShiftState is ShiftState.Closed)
                        continue;

                    await CheckUncompletedSales();

                    var shiftCounter = await _shiftCounterService.GetAsync(nozzle.Id, shift.Id);
                    var totalSales = await _fuelSaleService.GetReceivedQuantityAsync(nozzle.Id, shift.Id);
                    var unregisteredSales = await _unregisteredSaleService.GetAllAsync(nozzle.Id, shift.Id);

                    if (shiftCounter == null)
                        continue;

                    // Расчёт отклонения от начальных показаний
                    decimal unregisteredSalesQuantity = unregisteredSales != null ? unregisteredSales.Sum(u => u.Quantity) : 0;
                    decimal expectedCounter = shiftCounter.BeginSaleCounter + totalSales + unregisteredSalesQuantity;
                    decimal unregisteredQuantity = nozzle.LastCounter - (shiftCounter.BeginNozzleCounter + expectedCounter);

                    if (unregisteredQuantity != 0)
                    {
                        var unregisteredSale = new UnregisteredSale
                        {
                            NozzleId = nozzle.Id,
                            ShiftId = shift.Id,
                            CreateDate = DateTime.Now,
                            State = UnregisteredSaleState.Waiting,
                            Quantity = unregisteredQuantity,
                            Sum = unregisteredQuantity * nozzle.Price
                        };

                        await _unregisteredSaleService.CreateAsync(unregisteredSale);
                    }
                }
            }
            finally
            {
                _countersGate.Release();
            }
        }

        private async Task OnCounterUpdated(CounterData counterData)
        {
            var nozzle = Nozzles.FirstOrDefault(n => n.Group == counterData.GroupName);

            if (nozzle is null) return;

            nozzle.LastCounter = counterData.Counter;

            var shift = _shiftStore.CurrentShift;
            if (shift == null) return;

            if (_shiftStore.CurrentShiftState is ShiftState.Closed) return;

            await CheckUncompletedSales();

            var shiftCounter = await _shiftCounterService.GetAsync(nozzle.Id, shift.Id);
            var totalSales = await _fuelSaleService.GetReceivedQuantityAsync(nozzle.Id, shift.Id);
            var unregisteredSales = await _unregisteredSaleService.GetAllAsync(nozzle.Id, shift.Id);

            if (shiftCounter == null) return;

            // Расчёт отклонения от начальных показаний
            decimal unregisteredSalesQuantity = unregisteredSales != null ? unregisteredSales.Sum(u => u.Quantity) : 0;
            decimal expectedCounter = shiftCounter.BeginSaleCounter + totalSales + unregisteredSalesQuantity;
            decimal unregisteredQuantity = nozzle.LastCounter - (shiftCounter.BeginNozzleCounter + expectedCounter);

            if (unregisteredQuantity != 0)
            {
                var unregisteredSale = new UnregisteredSale
                {
                    NozzleId = nozzle.Id,
                    ShiftId = shift.Id,
                    CreateDate = DateTime.Now,
                    State = UnregisteredSaleState.Waiting,
                    Quantity = unregisteredQuantity,
                    Sum = unregisteredQuantity * nozzle.Price
                };

                await _unregisteredSaleService.CreateAsync(unregisteredSale);
            }
        }

        private async Task OnCompletedFuelingAsync(string groupName, decimal? quantity)
        {
            var nozzle = Nozzles.FirstOrDefault(n => n.Group == groupName);
            if (nozzle is null) return;

            var sale = SelectedNozzle.CurrentFuelSale;
            if (sale is null) return;

            try
            {
                //if (quantity != null)
                //{
                //    const decimal epsilon = 0.05m; // допуск ±0,05

                //    if (Math.Abs(CurrentFuelSale.Quantity - (quantity.Value + CurrentFuelSale.ResumeBaseQuantity)) > epsilon)
                //        return;
                //}

                sale.FuelSaleStatus = FuelSaleStatus.Completed;

                await HandleFiscalizationAsync(sale, nozzle);

                // Сохранение
                await _fuelSaleService.UpdateAsync(sale.Id, sale);
                _logger.LogInformation("Продажа {SaleId} завершена: {Status}", sale.Id, sale.FuelSaleStatus);

                // Запросить счетчики после завершения
                await _hub.InvokeAsync("GetCounterAsync", nozzle.Group);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при завершении заправки для продажи {SaleId}", SelectedNozzle.CurrentFuelSale.Id);
            }
        }

        private static bool IsFuelingCompleted(FuelSale sale)
        {
            // ReceivedCount — фактически залито
            // Quantity — запрос
            // Если типы decimal — лучше сравнивать с допуском.
            const decimal eps = 0.0005m; // допуск под 3 знака литров (0.001) и возможные округления
            return sale.ReceivedQuantity + eps >= sale.Quantity;
        }

        private async Task HandleFiscalizationAsync(FuelSale sale, Nozzle nozzle)
        {
            var printingMode = Properties.Settings.Default.ReceiptPrintingMode;

            // В режиме "После" — печатаем обычный чек после успешной заправки
            if (printingMode == "After")
            {
                await EnsureSaleCheckAsync(sale, nozzle);
                return;
            }

            // Иначе логика возврата/доприхода если недолив (запрос > факт)
            var isUnderfilling = sale.ReceivedQuantity + 0.0005m < sale.Quantity;
            if (!isUnderfilling) return;

            var isCashType = sale.PaymentType is PaymentType.Cash or PaymentType.Cashless;
            if (!isCashType) return;

            await EnsureReturnAndReceivedAsync(sale, nozzle);
        }

        private async Task EnsureSaleCheckAsync(FuelSale sale, Nozzle nozzle)
        {
            var fuel = sale.Tank?.Fuel;
            if (fuel is null)
            {
                MessageBoxService.ShowMessage("Не найдено топливо для продажи.", "Ошибка",
                    MessageButton.OK, MessageIcon.Error);
                return;
            }

            var createdFiscalData = sale.AfterCreateFiscalData(OperationType.Sale);
            var fiscalData = await _cashRegisterStore.SaleAsync(createdFiscalData);

            if (fiscalData is null)
            {
                MessageBoxService.ShowMessage("Не удалось получить фискальные данные от ККМ.", "Ошибка",
                    MessageButton.OK, MessageIcon.Error);
                return;
            }

            fiscalData = sale.UpdateFiscalData(fiscalData, nozzle, OperationType.Sale);
            await _fiscalDataService.CreateAsync(fiscalData);
        }

        private async Task EnsureReturnAndReceivedAsync(FuelSale sale, Nozzle nozzle)
        {
            var fuel = SelectedNozzle?.Tank?.Fuel ?? sale.Tank?.Fuel;
            if (fuel is null)
            {
                MessageBoxService.ShowMessage("Не найдено топливо для возврата/прихода.", "Ошибка",
                    MessageButton.OK, MessageIcon.Error);
                return;
            }


            var originalFiscalData = sale.FiscalDatas?.FirstOrDefault(fd => fd.OperationType == OperationType.Sale);

            if (originalFiscalData is not null)
            {
                var returnFiscalData = sale.CreateReturnFiscalData(nozzle, originalFiscalData);
                var returnedFiscalData = await _cashRegisterStore.ReturnAsync(returnFiscalData);
                await _fiscalDataService.CreateAsync(returnedFiscalData);

                var createFiscalData = sale.AfterCreateFiscalData(OperationType.Sale);
                var createdFiscalData = await _cashRegisterStore.SaleAsync(createFiscalData);
                await _fiscalDataService.CreateAsync(createdFiscalData);
            }
        }

        /// <summary>
        /// Обработчик события поднятия пистолета
        /// </summary>
        private void OnColumnLifted(string groupName, bool isLifted)
        {
            try
            {
                var nozzle = Nozzles.FirstOrDefault(n => n.Group == groupName);

                if (nozzle is null) return;

                if (isLifted)
                {
                    nozzle.Lifted = isLifted;
                }
                else
                {
                    foreach (var item in Nozzles)
                    {
                        item.Lifted = false;
                    }
                }
            }
            catch (Exception)
            {

            }
        }

        #endregion

        #region Private Voids

        private void RegisterHubHandlers()
        {
            if (_hubHandlersRegistered || _hub is null)
                return;

            _hub.Reconnecting += OnHubReconnecting;
            _hub.Reconnected += OnHubReconnected;
            _hub.Closed += OnHubClosed;
            _hubHandlersRegistered = true;

            _hubSubscriptions.Add(_hub.On<StatusResponse>("StatusChanged", e => OnStatusChanged(e)));
            _hubSubscriptions.Add(_hub.On<string, bool>("ColumnLiftedChanged", (groupName, isLifted) => OnColumnLifted(groupName, isLifted)));
            _hubSubscriptions.Add(_hub.On<WorkerStateNotification>("WorkerStateChanged", notification => OnWorkerStateChanged(notification)));
            _hubSubscriptions.Add(_hub.On<string, List<CounterData>>("OnCountersUpdated", (groupName, counterDatas) => OnCountersUpdated(counterDatas)));
            _hubSubscriptions.Add(_hub.On<CounterData>("OnCounterUpdated", (counterData) => OnCounterUpdated(counterData)));
            _hubSubscriptions.Add(_hub.On<string, decimal?>("OnCompletedFuelingAsync", (groupName, quantity) => OnCompletedFuelingAsync(groupName, quantity)));
            _hubSubscriptions.Add(_hub.On<string>("OnWaitingAsync", (groupName) => OnWaitingAsync(groupName)));
            _hubSubscriptions.Add(_hub.On<FuelingResponse>("OnPumpStopAsync", (response) => OnPumpStopAsync(response)));
            _hubSubscriptions.Add(_hub.On<FuelingResponse>("OnFuelingAsync", (response) => OnFuelingAsync(response)));
        }

        private void UnregisterHubHandlers()
        {
            if (_hub is null || !_hubHandlersRegistered)
                return;

            _hub.Reconnecting -= OnHubReconnecting;
            _hub.Reconnected -= OnHubReconnected;
            _hub.Closed -= OnHubClosed;

            foreach (var subscription in _hubSubscriptions)
            {
                subscription.Dispose();
            }

            _hubSubscriptions.Clear();
            _hubHandlersRegistered = false;
        }

        private Task OnHubReconnecting(Exception? error)
        {
            OnConnectionLost();
            MarkAllWorkersOffline();
            return Task.CompletedTask;
        }

        private async Task OnHubReconnected(string? connectionId)
        {
            Interlocked.Exchange(ref _connectionLostHandled, 0);
            await JoinAndSubscribeAsync();
        }

        private Task OnHubClosed(Exception? error)
        {
            OnConnectionLost();
            MarkAllWorkersOffline();
            return RestartHubConnectionLoopAsync();
        }

        private Task RestartHubConnectionLoopAsync()
        {
            if (_hub is null)
                return Task.CompletedTask;

            if (Interlocked.CompareExchange(ref _hubReconnectLoop, 1, 0) != 0)
                return Task.CompletedTask;

            return Task.Run(async () =>
            {
                try
                {
                    while (_hub.State != HubConnectionState.Connected)
                    {
                        try
                        {
                            await _hubClient.EnsureStartedAsync();
                            await JoinAndSubscribeAsync();
                            break;
                        }
                        catch
                        {
                            await Task.Delay(TimeSpan.FromSeconds(5));
                        }
                    }
                }
                finally
                {
                    Interlocked.Exchange(ref _hubReconnectLoop, 0);
                }
            });
        }

        private async Task JoinAndSubscribeAsync()
        {
            if (_hub is null || Nozzles is null || Nozzles.Count == 0)
                return;

            var groups = Nozzles
                .Select(n => n.Group)
                .Where(g => !string.IsNullOrWhiteSpace(g))
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            foreach (var group in groups)
            {
                await _hub.InvokeAsync("JoinController", group, false);
            }

            var nozzle = Nozzles.FirstOrDefault();

            if (nozzle != null)
            {
                await Task.Delay(2000);

                await _hub.InvokeAsync("InitializeConfigurationAsync", nozzle.Group);

                var prices = Nozzles.Select(n => new PriceRequest
                {
                    GroupName = n.Group,
                    Value = n.Tank.Fuel.Price
                }).ToList();

                await _hub.InvokeAsync("SetPricesAsync", prices);

                await _hub.InvokeAsync("GetCountersAsync", nozzle.Group);
            }

            await RequestWorkerStateSnapshotAsync(groups);
        }

        private void MarkAllWorkersOffline()
        {
            if (Nozzles is null)
                return;

            var now = DateTimeOffset.Now;
            foreach (var nozzle in Nozzles)
            {
                nozzle.Status = NozzleStatus.Unknown;
                nozzle.Lifted = false;
                nozzle.WorkerStateMessage = WorkerOfflineDueToHubMessage;
                nozzle.WorkerStateUpdatedAt = now;
            }
        }

        private async Task RequestWorkerStateSnapshotAsync(string[] groups)
        {
            if (_hub is null)
                return;

            if (groups is null || groups.Length == 0)
                return;

            try
            {
                var snapshot = await _hub.InvokeAsync<IReadOnlyCollection<WorkerStateNotification>>("GetWorkerStatesSnapshot", groups);
                await ApplyWorkerStates(snapshot);
            }
            catch
            {
                // намеренно проглатываем: потеря снимка не критична, события догонят позже
            }
        }

        private async Task ApplyWorkerStates(IEnumerable<WorkerStateNotification> states)
        {
            if (states is null)
                return;

            foreach (var state in states)
            {
                await ApplyWorkerState(state);
            }
        }

        private async Task OnWorkerStateChanged(WorkerStateNotification? notification)
        {
            await ApplyWorkerState(notification);

        }

        private async Task ApplyWorkerState(WorkerStateNotification? notification)
        {
            if (notification is null || Nozzles is null)
                return;

            try
            {
                var nozzle = Nozzles.FirstOrDefault(n => n.Group == notification.GroupName);

                if (nozzle is null) return;

                if (!notification.IsOnline)
                {
                    nozzle.Status = NozzleStatus.Unknown;
                    nozzle.Lifted = false;
                    if (notification is null || Nozzles is null)
                        return;
                }
                else
                {
                    // UI только подписывается на события состояния воркера
                    //await _hub.InvokeAsync("StartPolling", nozzle.Group);
                }
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// Обработчик события создания продажи
        /// </summary>
        private async void FuelSaleService_OnCreated(FuelSale fuelSale)
        {
            var nozzle = Nozzles.FirstOrDefault(n => n.Id == fuelSale.NozzleId);

            if (nozzle is null) return;

            nozzle.CurrentFuelSale = fuelSale;

            if (nozzle.CurrentFuelSale.DiscountSale != null)
            {
                //await _fuelDispenserService.SetPriceAsync(nozzle, fuelSale.DiscountSale.DiscountPrice);
                if (nozzle.CurrentFuelSale.IsForSum)
                {
                    //await _fuelDispenserService.StartFuelingSumAsync(nozzle, fuelSale.Sum + fuelSale.DiscountSale.DiscountSum);
                }
                else
                {
                    //await _fuelDispenserService.StartFuelingQuantityAsync(nozzle, fuelSale.Quantity + fuelSale.DiscountSale.DiscountQuantity);
                }
            }
            else
            {
                await _hub.InvokeAsync("SetPriceAsync", new PriceRequest { GroupName = nozzle.Group, Value = nozzle.Price });

                var fuelingRequest = new FuelingRequest
                {
                    GroupName = nozzle.Group,
                    Value = fuelSale.Sum,
                    FuelingStartMode = fuelSale.IsForSum ? FuelingStartMode.ByAmount : FuelingStartMode.ByVolume
                };

                await _hub.InvokeAsync("StartFuelingAsync", fuelingRequest);
            }
        }

        private async void FuelSaleService_OnResumeFueling(FuelSale fuelSale)
        {
            var nozzle = Nozzles.FirstOrDefault(n => n.Id == fuelSale.NozzleId);

            if (nozzle == null) return;

            nozzle.CurrentFuelSale = fuelSale;

            if (await ValidateFuelQuantity())
            {
                var fuelingRequest = new FuelingRequest
                {
                    GroupName = nozzle.Group,
                    Value = fuelSale.Sum - fuelSale.ReceivedSum,
                    FuelingStartMode = FuelingStartMode.ByAmount
                };

                await _hub.InvokeAsync("StartFuelingAsync", fuelingRequest);
            }
        }

        private void FuelSaleService_OnUpdated(FuelSale fuelSale)
        {
            if (fuelSale.FuelSaleStatus != FuelSaleStatus.Completed)
                return;

            if (SelectedNozzle != null &&
                SelectedNozzle.CurrentFuelSale != null &&
                SelectedNozzle.CurrentFuelSale.Id == fuelSale.Id)
            {
                SelectedNozzle.CurrentFuelSale.FuelSaleStatus = fuelSale.FuelSaleStatus;
            }

            Nozzle? nozzle = Nozzles.FirstOrDefault(f => f.Id == fuelSale.NozzleId);

            if (nozzle == null)
                return;

            Task.Run(async () =>
            {
                nozzle.SalesSum = await _fuelSaleService.GetReceivedQuantityAsync(nozzle.Id, _shiftStore.CurrentShift.Id);
            });
        }

        private void FiscalDataService_OnCreated(FiscalData fiscalData)
        {
            if (SelectedNozzle.CurrentFuelSale != null &&
                SelectedNozzle.CurrentFuelSale.Id == fiscalData.FuelSaleId)
            {
                SelectedNozzle.CurrentFuelSale.FiscalDatas.Add(fiscalData);
            }
        }

        private async void UserStore_OnLogout()
        {
            await StopAsync();
        }

        private ListBoxItem? FindListBoxItemFromEvent(MouseButtonEventArgs e)
        {
            DependencyObject current = e.OriginalSource as DependencyObject;
            while (current != null && current is not ListBoxItem)
                current = VisualTreeHelper.GetParent(current);

            return current as ListBoxItem;
        }

        #endregion

        #region Shift

        private async void ShiftStore_OnClosed(Shift shift)
        {
            var firstNozzle = Nozzles.First();

            await _hub.InvokeAsync("GetCountersAsync", firstNozzle.Group);

            //Ждем пол секунды
            await Task.Delay(1500);

            foreach (var nozzle in Nozzles)
            {
                ShiftCounter? nozzleCounter = await _shiftCounterService.GetAsync(nozzle.Id, shift.Id);

                if (nozzleCounter != null)
                {
                    nozzleCounter.EndNozzleCounter = nozzle.LastCounter;
                    nozzleCounter.EndSaleCounter = await _fuelSaleService.GetReceivedQuantityAsync(nozzle.Id, shift.Id);
                    await _shiftCounterService.UpdateAsync(nozzleCounter.Id, nozzleCounter);
                }
            }
        }

        private async void ShiftStore_OnOpened(Shift shift)
        {
            var firstNozzle = Nozzles.First();

            foreach (var nozzle in Nozzles)
            {
                await _hub.InvokeAsync("GetCountersAsync", nozzle.Group);
            }

            // Ждем пол секунды
            await Task.Delay(1500);

            foreach (var nozzle in Nozzles)
            {
                ShiftCounter? nozzleCounter = await _shiftCounterService.GetAsync(nozzle.Id, shift.Id);

                if (nozzleCounter == null)
                {
                    var shiftCounter = new ShiftCounter
                    {
                        NozzleId = nozzle.Id,
                        ShiftId = _shiftStore.CurrentShift.Id,
                        BeginNozzleCounter = nozzle.LastCounter,
                        BeginSaleCounter = await _fuelSaleService.GetReceivedQuantityAsync(nozzle.Id, shift.Id)
                    };
                    await _shiftCounterService.CreateAsync(shiftCounter);
                }
            }
        }

        private async Task CheckUncompletedSales()
        {
            try
            {
                var uncompletedSales = await _fuelSaleService.GetUncompletedFuelSaleAsync(_shiftStore.CurrentShift.Id);

                if (uncompletedSales == null)
                {
                    return;
                }

                foreach (var sale in uncompletedSales.Where(u => u.ReceivedQuantity > 0))
                {
                    if (sale.ReceivedQuantity == sale.Quantity && sale.FuelSaleStatus != FuelSaleStatus.Completed)
                    {
                        sale.FuelSaleStatus = FuelSaleStatus.Completed;
                        await _fuelSaleService.UpdateAsync(sale.Id, sale);
                    }
                }

            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// При авторизации
        /// </summary>
        /// <param name="shift"></param>
        private void ShiftStore_OnLogin(Shift shift)
        {
            _cts = new CancellationTokenSource();
            _startTask = StartAsync(_cts.Token);
        }

        #endregion

        #region Топлива

        private void FuelService_OnUpdated(Fuel fuel)
        {
            try
            {
                Nozzles.Where(n => n.Tank.FuelId == fuel.Id)
                       .Select(async n =>
                       {
                           n.Tank?.Fuel.Update(fuel);
                           await _hub.InvokeAsync("SetPriceAsync", n.Group, fuel.Price);
                       });
            }
            catch (Exception)
            {
                _logger.LogError("Ошибка при массовом обновлении цен");
            }
        }

        #endregion

        #region Nozzle

        /// <summary>
        /// Обработчик события выбора пистолета
        /// </summary>
        private void OnNozzleSelected(int tube)
        {
            if (SelectedNozzle == null)
            {
                SelectNozzle(tube);
                return;
            }

            if (SelectedNozzle.Tube != tube)
            {
                SelectNozzle(tube);
            }
        }

        private async void OnNozzleCountersRequested()
        {
            var nozzle = Nozzles.First();

            await _hub.InvokeAsync("GetCountersAsync", nozzle.Group);
        }

        /// <summary>
        /// Присваивает выбранный пистолет
        /// </summary>
        private void SelectNozzle(int tube)
        {
            Nozzle? newSelectedNozzle = Nozzles.FirstOrDefault(n => n.Tube == tube);
            if (newSelectedNozzle != null)
            {
                SelectedNozzle = newSelectedNozzle;
            }
        }

        /// <summary>
        /// Получить информацию о последнем продаже по пистолетам
        /// </summary>
        private async Task GetNozzleLastFuelSale()
        {
            if (_shiftStore.CurrentShiftState == ShiftState.None) return;

            FuelSale? globalLastSale = null;
            Nozzle? globalLastNozzle = null;

            foreach (Nozzle nozzle in Nozzles)
            {
                nozzle.SalesSum = await _fuelSaleService
                    .GetReceivedQuantityAsync(nozzle.Id, _shiftStore.CurrentShift.Id);

                FuelSale? fuelSale = await _fuelSaleService.GetLastFuelSale(nozzle.Id);
                if (fuelSale != null)
                {
                    nozzle.CurrentFuelSale = fuelSale;

                    // ---- Вот тут ищем глобальную последнюю запись ----
                    if (globalLastSale == null || fuelSale.CreateDate > globalLastSale.CreateDate)
                    {
                        globalLastSale = fuelSale;
                        globalLastNozzle = nozzle;
                    }
                }
            }

            // Если нашли самое свежее сопло — назначаем
            if (globalLastNozzle != null)
            {
                SelectedNozzle = globalLastNozzle;
            }
            else
            {
                if (Nozzles.Count > 0)
                {
                    SelectedNozzle = Nozzles[0];
                }
            }

            if (SelectedNozzle.CurrentFuelSale is not null)
            {
                var isCompleted = IsFuelingCompleted(SelectedNozzle.CurrentFuelSale!);
                if (isCompleted)
                {
                    SelectedNozzle.CurrentFuelSale.ResumeBaseQuantity = SelectedNozzle.CurrentFuelSale.ReceivedQuantity;
                    SelectedNozzle.CurrentFuelSale.ResumeBaseSum = SelectedNozzle.CurrentFuelSale.ReceivedSum;
                }
            }
        }

        private async void NozzleService_OnDeleted(int id)
        {
            var nozzle = Nozzles.FirstOrDefault(n => n.Id == id);

            if (nozzle == null) return;

            Nozzles.Remove(nozzle);

            if (!Nozzles.Any())
            {
                //await _hub.InvokeAsync("StopPolling", nozzle.Group);
                await _hub.InvokeAsync("LeaveController", nozzle.Group);
            }
        }

        private void NozzleService_OnUpdated(Nozzle updatedNozzle)
        {
            var nozzle = Nozzles.FirstOrDefault(n => n.Id == updatedNozzle.Id);

            if (nozzle == null) return;

            nozzle.Update(updatedNozzle);
            Side = updatedNozzle.Side;
        }

        private void NozzleService_OnCreated(Nozzle createdNozzle)
        {
            if (createdNozzle.Side == Side)
            {
                Nozzles.Add(createdNozzle);
            }
        }

        #endregion

        #region ККМ

        private async Task<bool> CanFuelSale(FuelSale fuelSale)
        {
            if (!ValidateNozzleSelection() || !await ValidateShift() || !ValidateCashRegisterShift()
                || !await ValidateFuelQuantity())
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
                return await _shiftStore.OpenShiftAsync();
            }

            if (result == MessageResult.No)
            {
                return false;
            }

            return true;
        }

        private bool ValidateCashRegisterShift()
        {
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
            IEnumerable<TankFuelQuantityView> tanks = await _tankFuelQuantityView.GetAllAsync();
            TankFuelQuantityView tank = tanks.First(t => t.Id == SelectedNozzle.TankId);

            if (tank.MinimumSize > 0)
            {
                if ((tank.CurrentFuelQuantity - SelectedNozzle.CurrentFuelSale.Quantity) <= tank.MinimumSize)
                {
                    MessageBoxService.ShowMessage("Недостаточно топлива в резервуаре с учетом мертвого остатка", "Внимание!", MessageButton.OK, MessageIcon.Exclamation);
                    return false;
                }
            }
            else
            {
                if ((tank.CurrentFuelQuantity - SelectedNozzle.CurrentFuelSale.Quantity) < 0)
                {
                    MessageBoxService.ShowMessage("Недостаточно топлива в резервуаре!", "Внимание!", MessageButton.OK, MessageIcon.Exclamation);
                    return false;
                }
            }
            return true;
        }

        #endregion

        #region Hot Keys

        private void HotKeysService_OnNumberKeyPressed(int number)
        {
            var isSelectionAllowed = Status switch
            {
                NozzleStatus.PumpWorking => true,
                NozzleStatus.WaitingStop => true,
                NozzleStatus.WaitingRemoved => true,
                _ => false
            };

            if (isSelectionAllowed) return;

            if (Nozzles != null)
            {
                var nozzle = Nozzles.FirstOrDefault(n => n.Tube == number);

                if (nozzle != null)
                {
                    SelectedNozzle = nozzle;
                    _nozzleStore.SelectNozzle(number);
                }
            }
        }

        #endregion
    }
}
