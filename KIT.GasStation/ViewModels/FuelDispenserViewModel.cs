using DevExpress.Mvvm.DataAnnotations;
using KIT.GasStation.Domain.Models;
using KIT.GasStation.Domain.Services;
using KIT.GasStation.FuelDispenser.Commands;
using KIT.GasStation.FuelDispenser.Hubs;
using KIT.GasStation.FuelDispenser.Models;
using KIT.GasStation.State.CashRegisters;
using KIT.GasStation.State.Nozzles;
using KIT.GasStation.State.Shifts;
using KIT.GasStation.State.Users;
using KIT.GasStation.ViewModels.Base;
using Microsoft.AspNetCore.SignalR.Client;
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
        private HubConnection _hub;
        private bool _canSelectedNozzle = true;
        private int _side;
        private NozzleStatus _status;
        private ObservableCollection<Nozzle> _nozzle = new();
        private Nozzle _selectedNozzle;
        private decimal _receivedQuantity;
        private decimal _receivedSum;
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
        public decimal ReceivedQuantity
        {
            get => _receivedQuantity;
            set
            {
                _receivedQuantity = value;
                OnPropertyChanged(nameof(ReceivedQuantity));
            }
        }
        public decimal ReceivedSum
        {
            get => _receivedSum;
            set
            {
                _receivedSum = value;
                OnPropertyChanged(nameof(ReceivedSum));
            }
        }
        public bool CanSelectedNozzle
        {
            get => _canSelectedNozzle;
            set
            {
                _canSelectedNozzle = value;
                OnPropertyChanged(nameof(CanSelectedNozzle));
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
            IHubClient hubClient)
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
            
            _shiftStore.OnLogin += ShiftStore_OnLogin;
            _fuelSaleService.OnCreated += FuelSaleService_OnCreated;
            _fuelSaleService.OnUpdated += FuelSaleService_OnUpdated;
            _nozzleStore.OnNozzleSelected += OnNozzleSelected;
            _nozzleStore.OnNozzleCountersRequested += OnNozzleCountersRequested;
            _shiftStore.OnOpened += ShiftStore_OnOpened;
            _shiftStore.OnClosed += ShiftStore_OnClosed;
            _fuelService.OnUpdated += FuelService_OnUpdated;
            _userStore.OnLogout += UserStore_OnLogout;
        }

        #endregion

        #region Public Voids

        /// <summary>
        /// Команда продолжения заправки
        /// </summary>
        /// <returns></returns>
        [Command]
        public async Task ContinueFilling()
        {
            //await _fuelDispenserService.ContinueFillingAsync(SelectedNozzle.Tube);
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
        public async Task CompleteFilling()
        {
            try
            {
                //await _fuelDispenserService.CompleteFillingAsync(SelectedNozzle.Tube);

                if (SelectedNozzle.Sum > SelectedNozzle.ReceivedSum)
                {
                    await _cashRegisterStore.ReturnAndReceivedSaleAsync(SelectedNozzle.FuelSale, SelectedNozzle.Tank.Fuel);
                    await _fuelSaleService.UpdateAsync(SelectedNozzle.FuelSale.Id, SelectedNozzle.FuelSale);
                }
            }
            catch (Exception)
            {
                
            }
        }

        /// <summary>
        /// Команда остановки заправки
        /// </summary>
        [Command]
        public async Task StopFilling()
        {
            //await _fuelDispenserService.StopRefuelingAsync(SelectedNozzle);
        }

        [Command]
        public async Task ChangeControlMode()
        {
            if (SelectedNozzle != null)
            {
                await ChangeControlModeAsync(SelectedNozzle);
            }
        }

        #endregion

        #region ТРК Команды

        private async Task StartAsync(CancellationToken token)
        {
            try
            {
                if (Nozzles.Count == 0) return;

                _hub = _hubClient.Connection;

                RegisterHubHandlers();

                await _hubClient.EnsureStartedAsync(token);

                await JoinAndStartPollingAsync();

                //// важно: после переподключения — заново join
                //hub.Reconnected += _ => hub.InvokeAsync("JoinController", "jf", 0);

                //_fuelDispenserService = await _fuelDispenserFactory.CreateAsync(Nozzles);

                //if (_fuelDispenserService == null) return;

                // Получаем информацию о последних продажах по пистолетам
                await GetNozzleLastFuelSale();

                //_fuelDispenserService.OnStatusChanged += OnStatusChanged;
                //_fuelDispenserService.OnCounterReceived += OnCounterReceived;
                //_fuelDispenserService.OnColumnLifted += OnColumnLifted;
                //_fuelDispenserService.OnColumnLowered += OnColumnLowered;
                //_fuelDispenserService.OnStartedFilling += OnStartedFilling;
                //_fuelDispenserService.OnWaitingRemoved += OnWaitingRemoved;
                //_fuelDispenserService.OnCompletedFilling += OnCompletedFilling;
                //_fuelDispenserService.OnConnectionLost += OnConnectionLost;


                // Подключаемся к ТРК
                //await _fuelDispenserService.Connect(Nozzles);

                // Инициализия ТРК
                //await _fuelDispenserService.InitializeAsync(Side);

                // Устанавливаем цену
                foreach (var item in Nozzles)
                {
                    token.ThrowIfCancellationRequested();
                    //await _fuelDispenserService.SetPriceAsync(item);
                }

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

        private async Task StopAsync()
        {
            try
            {
                _cts?.Cancel();

                if (_startTask != null)
                {
                    await _startTask; // Ждём завершения StartAsync
                }

                //_fuelDispenserService?.Dispose();
                //_fuelDispenserService = null;
            }
            catch (Exception e)
            {
                // Логируй ошибку
            }
        }

        private async Task ChangeAllControlModeAsync()
        {
            try
            {
                if (Nozzles != null)
                {
                    foreach (var item in Nozzles)
                    {
                        await ChangeControlModeAsync(item);
                    }
                }
            }
            catch (Exception)
            {

            }
        }

        private async Task ChangeControlModeAsync(Nozzle nozzle)
        {
            await _hub.InvokeAsync("ChangeControlModeAsync", nozzle.Group, true);
        }

        /// <summary>
        /// Обработчик события завершения заправки
        /// </summary>
        private async Task OnCompletedFilling(Nozzle nozzle, ControllerResponse response)
        {
            try
            {
                decimal quantity = response.Quantity;
                decimal sum = response.Sum;

                SelectedNozzle.FuelSale.ReceivedQuantity = quantity;
                SelectedNozzle.FuelSale.ReceivedSum = sum;
                ReceivedQuantity = quantity;
                ReceivedSum = sum;
                nozzle.FuelSale.FuelSaleStatus = FuelSaleStatus.Completed;
                await _fuelSaleService.UpdateAsync(nozzle.FuelSale.Id, nozzle.FuelSale);
                await _hub.InvokeAsync("GetCountersAsync", nozzle.Group);
            }
            catch (Exception e)
            {

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

            nozzle.FuelSale.FuelSaleStatus = FuelSaleStatus.InProgress;
            await _fuelSaleService.UpdateAsync(nozzle.FuelSale.Id, nozzle.FuelSale);
        }

        /// <summary>
        /// Обработчик события заполнения определенного объема
        /// </summary>
        private async Task OnStartedFilling(ControllerResponse deviceResponse)
        {
            decimal quantity = deviceResponse.Quantity;
            decimal sum = deviceResponse.Sum;

            if (quantity == 0) return;

            var nozzle = Nozzles.FirstOrDefault(n => n.Group == deviceResponse.Group);

            if (nozzle is null) return;

            if (SelectedNozzle == null) return;

            if (SelectedNozzle.FuelSale == null) return;

            if (SelectedNozzle.FuelSale.FuelSaleStatus == FuelSaleStatus.None)
            {
                SelectedNozzle.FuelSale.FuelSaleStatus = FuelSaleStatus.InProgress;
            }

            if (SelectedNozzle.FuelSale.FuelSaleStatus == FuelSaleStatus.InProgress)
            {
                SelectedNozzle.FuelSale.ReceivedQuantity = quantity;
                SelectedNozzle.FuelSale.ReceivedSum = sum;
                ReceivedQuantity = quantity;
                ReceivedSum = sum;
            }

            await _fuelSaleService.UpdateAsync(SelectedNozzle.FuelSale.Id, SelectedNozzle.FuelSale);

            SelectedNozzle.FuelSale.FuelSaleStatus = FuelSaleStatus.InProgress;
            await _fuelSaleService.UpdateAsync(SelectedNozzle.FuelSale.Id, SelectedNozzle.FuelSale);
        }

        /// <summary>
        /// Обработчик события изменения статуса колонки
        /// </summary>ControllerResponse deviceResponse
        private async Task OnStatusChanged(ControllerResponse deviceResponse)
        {
            var nozzle = Nozzles.FirstOrDefault(n => n.Group == deviceResponse.Group);
            if (nozzle is null)
            {
                return;
            }

            Status = deviceResponse.Status;

            nozzle.Status = Status;
            switch (Status)
            {
                case NozzleStatus.PumpWorking:
                    await OnStartedFilling(deviceResponse);
                    break;
                case NozzleStatus.WaitingRemoved:
                    await OnWaitingRemoved(nozzle);
                    break;
                case NozzleStatus.PumpStop:
                    await _hub.InvokeAsync("CompleteRefuelingAsync", nozzle.Group);
                    break;
                case NozzleStatus.WaitingStop:
                    await _hub.InvokeAsync("CompleteRefuelingAsync", nozzle.Group);
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
                case Command.CompleteFilling:
                    await OnCompletedFilling(nozzle, deviceResponse);
                    break;
                case Command.ProgramControlMode:
                    var sa = 0;
                    break;
                case Command.KeyboardControlMode:
                    var sass = 0;
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
            if (shift == null || _shiftStore.CurrentShiftState is not (ShiftState.Open or ShiftState.Exceeded24Hours))
                return;

            await CheckUncompletedSales();

            var shiftCounter = await _shiftCounterService.GetAsync(nozzle.Id, shift.Id);
            var totalSales = await _fuelSaleService.GetReceivedQuantityAsync(nozzle.Id, shift.Id);
            var unregisteredSales = await _unregisteredSaleService.GetAllAsync(nozzle.Id, shift.Id);

            if (shiftCounter == null)
                return;

            // Расчёт отклонения от начальных показаний
            decimal unregisteredSalesSum = unregisteredSales != null ? unregisteredSales.Sum(u => u.Quantity) : 0;
            decimal expectedCounter = shiftCounter.BeginSaleCounter + totalSales + unregisteredSalesSum;
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

        #endregion

        #region Private Members

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

                await ChangeAllControlModeAsync();

                await _hub.InvokeAsync("StartPolling", group);
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
        private void FuelSaleService_OnCreated(FuelSale fuelSale)
        {
            var nozzle = Nozzles.FirstOrDefault(n => n.Id == fuelSale.NozzleId);

            if (nozzle is null) return;

            nozzle.FuelSale = fuelSale;

            Task.Run(async () =>
            {
                if (fuelSale.DiscountSale != null)
                {
                    //await _fuelDispenserService.SetPriceAsync(nozzle, fuelSale.DiscountSale.DiscountPrice);
                    if (fuelSale.IsForSum)
                    {
                        //await _fuelDispenserService.StartRefuelingSumAsync(nozzle, fuelSale.Sum + fuelSale.DiscountSale.DiscountSum);
                    }
                    else
                    {
                        //await _fuelDispenserService.StartRefuelingQuantityAsync(nozzle, fuelSale.Quantity + fuelSale.DiscountSale.DiscountQuantity);
                    }
                }
                else
                {
                    await _hub.InvokeAsync("SetPriceAsync", nozzle.Group, nozzle.Price);
                    if (fuelSale.IsForSum)
                    {
                        await _hub.InvokeAsync("StartRefuelingAsync", nozzle.Group, fuelSale.Sum, true);
                    }
                    else
                    {
                        await _hub.InvokeAsync("StartRefuelingAsync", nozzle.Group, fuelSale.Quantity, false);
                    }
                }
            });

            CanSelectedNozzle = false;
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

            CanSelectedNozzle = true;
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

        private void ShiftStore_OnClosed(Shift shift)
        {
            Task.Run(async () =>
            {
                foreach (var nozzle in Nozzles)
                {
                    //await _fuelDispenserService.GetCountersAsync(nozzle);
                }

                //Ждем пол секунды
                await Task.Delay(1000);

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
            });
        }

        private void ShiftStore_OnOpened(Shift shift)
        {
            Task.Run(async () =>
            {
                foreach (var nozzle in Nozzles)
                {
                    //await _fuelDispenserService.GetCountersAsync(nozzle);
                }

                // Ждем пол секунды
                await Task.Delay(1000);

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
            });
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
            _ = Task.Run(async () =>
            {
                try
                {
                    var tasks = Nozzles
                        .Where(n => n.Tank.FuelId == fuel.Id)
                        .Select(async nozzle =>
                        {
                            nozzle.Tank.Fuel.Update(fuel);
                            //await _fuelDispenserService.SetPriceAsync(nozzle);
                        });

                    await Task.WhenAll(tasks);
                }
                catch (Exception ex)
                {
                   //Log.Error(ex, "Ошибка при массовом обновлении цен");
                }
            });
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

        private void OnNozzleCountersRequested()
        {
            Task.Run(async () =>
            {

                foreach (var nozzle in Nozzles)
                {
                    //await _fuelDispenserService.GetCountersAsync(nozzle);
                }

            });
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
            if (_shiftStore.CurrentShiftState != ShiftState.None)
            {
                foreach (Nozzle nozzle in Nozzles)
                {
                    nozzle.SalesSum = await _fuelSaleService.GetReceivedQuantityAsync(nozzle.Id, _shiftStore.CurrentShift.Id);

                    FuelSale? fuelSale = await _fuelSaleService.GetLastFuelSale(nozzle.Id, _shiftStore.CurrentShift.Id);
                    if (fuelSale != null)
                    {
                        nozzle.FuelSale = fuelSale;
                    }
                }
            }
        }

        #endregion

        #region Dispose

        protected override async void Dispose(bool disposing)
        {
            _shiftStore.OnLogin -= ShiftStore_OnLogin;
            _fuelSaleService.OnCreated -= FuelSaleService_OnCreated;
            _fuelSaleService.OnUpdated -= FuelSaleService_OnUpdated;
            _nozzleStore.OnNozzleSelected -= OnNozzleSelected;
            _nozzleStore.OnNozzleCountersRequested -= OnNozzleCountersRequested;
            _shiftStore.OnOpened -= ShiftStore_OnOpened;
            _shiftStore.OnClosed -= ShiftStore_OnClosed;
            _fuelService.OnUpdated -= FuelService_OnUpdated;
            _userStore.OnLogout -= UserStore_OnLogout;

            await StopAsync();

            base.Dispose(disposing);
        }

        #endregion
    }
}
