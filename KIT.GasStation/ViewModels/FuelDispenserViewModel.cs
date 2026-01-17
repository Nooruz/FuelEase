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
using System.Collections.Concurrent;
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
    public class FuelDispenserViewModel : BaseViewModel
    {
        #region Private Members

        private readonly INozzleStore _nozzleStore;
        private readonly IFuelSaleService _fuelSaleService;
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
        private readonly ConcurrentDictionary<Guid, NozzleStatus> _lastStatuses = new();
        private CancellationTokenSource _cts;
        private Task _startTask;
        private bool _hubHandlersRegistered;
        private int _hubReconnectLoop;
        private const string WorkerOfflineDueToHubMessage = "Нет связи с сервером";

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
            get
            {
                if (SelectedNozzle == null) return 0;
                if (SelectedNozzle.FuelSale == null) return 0;
                return SelectedNozzle.FuelSale.ReceivedSum;
            }
            set
            {
                if (SelectedNozzle == null) return;
                if (SelectedNozzle.FuelSale == null) return;
                SelectedNozzle.FuelSale.ReceivedSum = value;
                OnPropertyChanged(nameof(ReceivedSum));
            }
        }
        public decimal ReceivedQuantity
        {
            get
            {
                if (SelectedNozzle == null) return 0;
                if (SelectedNozzle.FuelSale == null) return 0;
                return SelectedNozzle.FuelSale.ReceivedQuantity;
            }
            set
            {
                if (SelectedNozzle == null) return;
                if (SelectedNozzle.FuelSale == null) return;
                SelectedNozzle.FuelSale.ReceivedQuantity = value;
                OnPropertyChanged(nameof(ReceivedQuantity));
            }
        }

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

                await _hub.InvokeAsync("ResumeFuelingAsync", SelectedNozzle?.Group);

                _logger.LogInformation("Команда ResumeFueling отправлена для группы {Group}", SelectedNozzle?.Group);
            }
            catch(HubException ex)
            {
                _logger.LogError(ex, "HubException при вызове ResumeFuelingAsync: {Message}", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при вызове ResumeFuelingAsync для пистолета {NozzleGroup}", SelectedNozzle?.Group ?? "null");
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
            catch (Exception ex)
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
                        Sum = 100 * SelectedNozzle.Tank.Fuel.Price,
                        Quantity = 100,
                        Price = SelectedNozzle.Tank.Fuel.Price,
                        ReceivedCount = SelectedNozzle.LastCounter,
                        IsForSum = false,
                        CreateDate = DateTime.Now,
                        PaymentType = PaymentType.Cash,
                    };


                    if (Properties.Settings.Default.ReceiptPrintingMode == "Before")
                    {
                        var fiscalData = _cashRegisterStore.SaleAsync(fuelSale, SelectedNozzle.Tank.Fuel);

                        if (fiscalData != null)
                        {
                            fuelSale.FiscalData = await fiscalData;
                            await _fuelSaleService.CreateAsync(fuelSale);
                        }
                        else
                        {
                            if (fuelSale.FiscalData is not null)
                            {
                                await _fuelSaleService.CreateAsync(fuelSale);
                            }
                            else
                            {
                                MessageBoxService.ShowMessage("Не удалось получить фискальные данные от ККМ.", "Ошибка", MessageButton.OK, MessageIcon.Error);
                            }
                        }
                    }
                    else
                    {
                        await _fuelSaleService.CreateAsync(fuelSale);
                    }
                }
            }
            catch (Exception)
            {

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

                await JoinAndStartPollingAsync();

                // Запускаем цикл опроса статуса
                //await _fuelDispenserService.StartStatusPolling(0);
            }
            catch (OperationCanceledException)
            {
                // Ожидаемая отмена, можно проигнорировать
            }
            catch (Exception e)
            {

            }
        }

        public async Task StopAsync()
        {
            try
            {

                foreach (var item in Nozzles)
                {
                    await _hub.InvokeAsync("StopPolling", item.Group);
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
            try
            {
                await PausePollingAsync(nozzle.Group);
                
                await _hub.InvokeAsync("ChangeControlModeAsync", nozzle.Group, true);
            }
            finally
            {
                await ResumePollingAsync(nozzle.Group);
            }
        }

        /// <summary>
        /// Обработчик события завершения заправки
        /// </summary>
        private async Task OnCompletedFueling(Nozzle nozzle, ControllerResponse response)
        {
            if (response.Address == response.StatusAddress)
            {
                _logger.LogDebug("OnCompletedFueling для пистолета {NozzleId}, статус: {Status}", nozzle.Id, response.Status);

                var fuelSale = SelectedNozzle?.FuelSale;

                if (fuelSale == null)
                {
                    _logger.LogWarning("Нет активной продажи для завершения заправки на пистолете {NozzleId}",
                        nozzle.Id);
                    return;
                }

                try
                {
                    decimal quantity = response.Quantity;
                    decimal sum = response.Sum;

                    ReceivedQuantity = quantity;
                    ReceivedSum = sum;

                    if (Properties.Settings.Default.ReceiptPrintingMode == "After")
                    {
                        if (fuelSale.FiscalData == null)
                        {
                            var fiscalData = await _cashRegisterStore.SaleAsync(fuelSale, fuelSale.Tank.Fuel, false);

                            if (fiscalData is not null)
                            {
                                fuelSale.FiscalData ??= fiscalData;
                            }
                            else
                            {
                                MessageBoxService.ShowMessage("Не удалось получить фискальные данные от ККМ.", "Ошибка", MessageButton.OK, MessageIcon.Error);
                            }
                        }
                    }
                    else
                    {
                        if (fuelSale.Quantity > fuelSale.ReceivedQuantity)
                        {
                            if (fuelSale.PaymentType is PaymentType.Cash or PaymentType.Cashless)
                            {
                                if (fuelSale.FiscalData?.ReturnCheck == null)
                                {
                                    var returntFiscalData = await _cashRegisterStore
                                    .ReturnAndReceivedSaleAsync(SelectedNozzle.FuelSale, SelectedNozzle.Tank.Fuel, _userStore.CurrentUser.FullName);

                                    if (returntFiscalData != null)
                                    {
                                        fuelSale.FiscalData.ReturnCheck ??= returntFiscalData.Check;
                                    }
                                }  
                            }
                        }
                    }

                    if (fuelSale.FuelSaleStatus is not FuelSaleStatus.Completed)
                    {
                        fuelSale.FuelSaleStatus = FuelSaleStatus.Completed;
                        await _fuelSaleService.UpdateAsync(fuelSale.Id, fuelSale);
                    }

                    _logger.LogInformation("Продажа {SaleId} завершена успешно", fuelSale.Id);

                    await PausePollingAsync(nozzle.Group);

                    await _hub.InvokeAsync("GetCountersAsync", nozzle.Group);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при завершении заправки для продажи {SaleId}", fuelSale.Id);
                }
                finally
                {
                    await ResumePollingAsync(nozzle.Group);
                }
            }
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
        private async Task OnWaitingRemoved(Nozzle nozzle)
        {
            if (nozzle.FuelSale == null) return;

            nozzle.FuelSale.FuelSaleStatus = FuelSaleStatus.InProcessed;
            await _fuelSaleService.UpdateAsync(nozzle.FuelSale.Id, nozzle.FuelSale);
        }

        private async Task OnPumpStop(Nozzle nozzle, ControllerResponse response)
        {
            try
            {
                await PausePollingAsync(nozzle.Group);

                if (response.Address == response.StatusAddress)
                {
                    await _hub.InvokeAsync("CompleteFuelingAsync", nozzle.Group);
                }
                else
                {
                    await _hub.InvokeAsync("GetStatusByAddressAsync", nozzle.Group);
                }
            }
            finally
            {
                await ResumePollingAsync(nozzle.Group);
            }
        }

        /// <summary>
        /// Обработчик события ожидания остановки заправки
        /// </summary>
        private async Task OnWaitingStop(Nozzle nozzle)
        {
            if (nozzle.FuelSale == null) return;

            nozzle.FuelSale.FuelSaleStatus = FuelSaleStatus.Processed;
            await _fuelSaleService.UpdateAsync(nozzle.FuelSale.Id, nozzle.FuelSale);
        }

        /// <summary>
        /// Обработчик события заполнения определенного объема
        /// </summary>
        private void OnStartedFueling(ControllerResponse deviceResponse)
        {
            if (SelectedNozzle == null) return;

            if (SelectedNozzle.FuelSale == null) return;

            if (deviceResponse.Quantity == 0) return;

            ReceivedQuantity = deviceResponse.Quantity;
            ReceivedSum = deviceResponse.Sum;
        }

        /// <summary>
        /// Обработчик события изменения статуса колонки
        /// </summary>ControllerResponse deviceResponse
        private async Task OnStatusChanged(ControllerResponse deviceResponse)
        {
            var nozzle = Nozzles.FirstOrDefault(n => n.Group == deviceResponse.Group);
            if (nozzle is null) return;

            Status = deviceResponse.Status;

            nozzle.Status = Status;
            switch (Status)
            {
                case NozzleStatus.PumpWorking:
                    OnStartedFueling(deviceResponse);
                    break;
                case NozzleStatus.WaitingRemoved:
                    await OnWaitingRemoved(nozzle);
                    break;
                case NozzleStatus.PumpStop:
                    await OnPumpStop(nozzle, deviceResponse);
                    break;
                case NozzleStatus.WaitingStop:
                    await OnWaitingStop(nozzle);
                    break;
                case NozzleStatus.Blocking:
                    break;
                default:
                    break;
            }

            switch (deviceResponse.Command)
            {
                case Command.CounterLiter:
                    await OnCounterReceived(deviceResponse);
                    break;
                case Command.CompleteFueling:
                    await OnCompletedFueling(nozzle, deviceResponse);
                    break;
                case Command.ProgramControlMode:
                    foreach (var item in Nozzles)
                    {
                        item.IsProgramControl = false;
                    }
                    break;
                case Command.KeyboardControlMode:
                    foreach (var item in Nozzles)
                    {
                        item.IsProgramControl = true;
                    }
                    break;
            }

            if (Status == NozzleStatus.Ready)
            {
                foreach (Nozzle item in Nozzles)
                {
                    item.Status = NozzleStatus.Ready;
                }
            }
        }

        /// <summary>
        /// Обработчик события получения счетчика
        /// </summary>
        private async Task OnCounterReceived(ControllerResponse deviceResponse)
        {
            var nozzle = Nozzles.FirstOrDefault(n => n.Group == deviceResponse.Group);

            if (nozzle is null) return;

            nozzle.LastCounter = deviceResponse.Quantity;

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

        private async Task ResumePollingAsync(string groupName)
        {
            await _hub.InvokeAsync("ResumePollingAsync", groupName);
        }

        private async Task PausePollingAsync(string groupName)
        {
            await _hub.InvokeAsync("PausePollingAsync", groupName);
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

            _hub.On<ControllerResponse>("StatusChanged", e => OnStatusChanged(e));
            _hub.On<string, bool>("ColumnLiftedChanged", (groupName, isLifted) => OnColumnLifted(groupName, isLifted));
            _hub.On<WorkerStateNotification>("WorkerStateChanged", notification => OnWorkerStateChanged(notification));
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
            await JoinAndStartPollingAsync();
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
                            await _hub.StartAsync();
                            await JoinAndStartPollingAsync();
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

        private async Task JoinAndStartPollingAsync()
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
                await _hub.InvokeAsync("StartPolling", nozzle.Group);

                try
                {
                    await Task.Delay(3000);

                    await PausePollingAsync(nozzle.Group);

                    await _hub.InvokeAsync("ChangeControlModeAsync", nozzle.Group, true);

                    foreach (var item in Nozzles)
                    {
                        await _hub.InvokeAsync("GetCountersAsync", item.Group);
                    }

                    // Устанавливаем цену
                    foreach (var item in Nozzles)
                    {
                        await _hub.InvokeAsync("SetPriceAsync", item.Group, item.Tank.Fuel.Price);
                    }

                }
                finally
                {
                    await ResumePollingAsync(nozzle.Group);
                }
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
                    await _hub.InvokeAsync("StartPolling", nozzle.Group);
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
            if (fuelSale.OperationType is not OperationType.Sale) return;

            var nozzle = Nozzles.FirstOrDefault(n => n.Id == fuelSale.NozzleId);

            if (nozzle is null) return;

            nozzle.FuelSale = fuelSale;

            if (fuelSale.DiscountSale != null)
            {
                //await _fuelDispenserService.SetPriceAsync(nozzle, fuelSale.DiscountSale.DiscountPrice);
                if (fuelSale.IsForSum)
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
                try
                {
                    await PausePollingAsync(nozzle.Group);

                    await _hub.InvokeAsync("SetPriceAsync", nozzle.Group, nozzle.Price);
                    if (fuelSale.IsForSum)
                    {
                        await _hub.InvokeAsync("StartFuelingAsync", nozzle.Group, fuelSale.Sum, true);
                    }
                    else
                    {
                        await _hub.InvokeAsync("StartFuelingAsync", nozzle.Group, fuelSale.Quantity, false);
                    }
                }
                finally
                {
                    await ResumePollingAsync(nozzle.Group);
                }
            }
        }

        private async void FuelSaleService_OnResumeFueling(FuelSale fuelSale)
        {
            var nozzle = Nozzles.FirstOrDefault(n => n.Id == fuelSale.NozzleId);

            if (nozzle == null) return;

            nozzle.FuelSale = fuelSale;

            if (await ValidateFuelQuantity(fuelSale))
            {
                try
                {
                    await PausePollingAsync(nozzle.Group);

                    await _hub.InvokeAsync("StartFuelingAsync", nozzle.Group, fuelSale.Sum - fuelSale.ReceivedSum, true);
                }
                finally
                {
                    await ResumePollingAsync(nozzle.Group);
                }
            }
        }

        private void FuelSaleService_OnUpdated(FuelSale fuelSale)
        {
            if (fuelSale.FuelSaleStatus != FuelSaleStatus.Completed)
                return;

            Nozzle? nozzle = Nozzles.FirstOrDefault(f => f.Id == fuelSale.NozzleId);

            if (nozzle == null)
                return;

            Task.Run(async () =>
            {
                nozzle.SalesSum = await _fuelSaleService.GetReceivedQuantityAsync(nozzle.Id, _shiftStore.CurrentShift.Id);
            });
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

            try
            {
                await PausePollingAsync(firstNozzle.Group);

                foreach (var nozzle in Nozzles)
                {
                    await _hub.InvokeAsync("GetCountersAsync", nozzle.Group);
                }
            }
            finally
            {
                await ResumePollingAsync(firstNozzle.Group);
            }
            

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

            try
            {
                await PausePollingAsync(firstNozzle.Group);

                foreach (var nozzle in Nozzles)
                {
                    await _hub.InvokeAsync("GetCountersAsync", nozzle.Group);
                }
            }
            finally
            {
                await ResumePollingAsync(firstNozzle.Group);
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
                           try
                           {
                               await PausePollingAsync(n.Group);

                               n.Tank?.Fuel.Update(fuel);
                               await _hub.InvokeAsync("SetPriceAsync", n.Group, fuel.Price);
                           }
                           finally
                           {
                               await ResumePollingAsync(n.Group);
                           }
                       });
            }
            catch (Exception ex)
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

            try
            {
                await PausePollingAsync(nozzle.Group);

                foreach (var item in Nozzles)
                {
                    await _hub.InvokeAsync("GetCountersAsync", item.Group);
                }

            }
            finally
            {
                await ResumePollingAsync(nozzle.Group);
            }
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
                    nozzle.FuelSale = fuelSale;

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
        }

        private async void NozzleService_OnDeleted(int id)
        {
            var nozzle = Nozzles.FirstOrDefault(n => n.Id == id);

            if (nozzle == null) return;

            Nozzles.Remove(nozzle);

            if (!Nozzles.Any())
            {
                await _hub.InvokeAsync("StopPolling", nozzle.Group);
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
                || !await ValidateFuelQuantity(fuelSale))
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

        private async Task<bool> ValidateFuelQuantity(FuelSale fuelSale)
        {
            IEnumerable<TankFuelQuantityView> tanks = await _tankFuelQuantityView.GetAllAsync();
            TankFuelQuantityView tank = tanks.First(t => t.Id == SelectedNozzle.TankId);

            if (tank.MinimumSize > 0)
            {
                if ((tank.CurrentFuelQuantity - fuelSale.Quantity) <= tank.MinimumSize)
                {
                    MessageBoxService.ShowMessage("Недостаточно топлива в резервуаре с учетом мертвого остатка", "Внимание!", MessageButton.OK, MessageIcon.Exclamation);
                    return false;
                }
            }
            else
            {
                if ((tank.CurrentFuelQuantity - fuelSale.Quantity) < 0)
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
