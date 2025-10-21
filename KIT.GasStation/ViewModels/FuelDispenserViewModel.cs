using DevExpress.Mvvm.DataAnnotations;
using KIT.GasStation.Domain.Models;
using KIT.GasStation.Domain.Services;
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

        #endregion

        #region Private Members

        private async Task StartAsync(CancellationToken token)
        {
            try
            {
                if (Nozzles.Count == 0) return;

                var hub = _hubClient.Connection;

                hub.On<ControllerResponse>("StatusChanged", e => OnStatusChanged(e));

                await _hubClient.EnsureStartedAsync();

                foreach (var item in Nozzles)
                {
                    await hub.InvokeAsync("JoinController", item.Group);
                    await hub.InvokeAsync("StartPolling", item.Group);
                }

                //// важно: после переподключения — заново join
                //hub.Reconnected += _ => hub.InvokeAsync("JoinController", "jf", 0);

                //_fuelDispenserService = await _fuelDispenserFactory.CreateAsync(Nozzles);

                //if (_fuelDispenserService == null) return;

                //// Получаем информацию о последних продажах по пистолетам
                //await GetNozzleLastFuelSale();

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

        /// <summary>
        /// Обработчик события завершения заправки
        /// </summary>
        private void OnCompletedFilling(int id)
        {
            Nozzle? nozzle = Nozzles.FirstOrDefault(n => n.Id == id);

            if (nozzle == null)
                return;

            Task.Run(async () =>
            {
                nozzle.FuelSale.FuelSaleStatus = FuelSaleStatus.Completed;
                await _fuelSaleService.UpdateAsync(nozzle.FuelSale.Id, nozzle.FuelSale);
                //await _fuelDispenserService.SetPriceAsync(nozzle);
                //await _fuelDispenserService.GetCountersAsync(nozzle);
            });

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
        private void OnWaitingRemoved(int id)
        {
            Nozzle? nozzle = Nozzles.FirstOrDefault(n => n.Id == id);

            if (nozzle == null)
                return;

            if (nozzle.FuelSale == null)
                return;

            Task.Run(async () =>
            {
                nozzle.FuelSale.FuelSaleStatus = FuelSaleStatus.InProgress;
                await _fuelSaleService.UpdateAsync(nozzle.FuelSale.Id, nozzle.FuelSale);
            });
        }

        /// <summary>
        /// Обработчик события заполнения определенного объема
        /// </summary>
        private void OnStartedFilling(Guid columnId, decimal sum, decimal quantity)
        {
            //if (quantity == 0)
            //{
            //    return;
            //}

            //if (SelectedNozzle.FuelSale == null)
            //    return;

            //if (SelectedNozzle.FuelSale.FuelSaleStatus == FuelSaleStatus.None)
            //{
            //    SelectedNozzle.FuelSale.FuelSaleStatus = FuelSaleStatus.InProgress;
            //}

            //if (SelectedNozzle.FuelSale.FuelSaleStatus == FuelSaleStatus.InProgress)
            //{
            //    SelectedNozzle.FuelSale.ReceivedQuantity = quantity;
            //    SelectedNozzle.FuelSale.ReceivedSum = sum;
            //    ReceivedQuantity = quantity;
            //    ReceivedSum = sum;
            //}

            //Task.Run(async () =>
            //{
            //    await _fuelSaleService.UpdateAsync(SelectedNozzle.FuelSale.Id, SelectedNozzle.FuelSale);

            //    SelectedNozzle.FuelSale.FuelSaleStatus = FuelSaleStatus.InProgress;
            //    await _fuelSaleService.UpdateAsync(SelectedNozzle.FuelSale.Id, SelectedNozzle.FuelSale);
            //});
        }

        /// <summary>
        /// Обработчик события изменения статуса колонки
        /// </summary>
        private async Task OnStatusChanged(ControllerResponse deviceResponse)
        {
            Status = deviceResponse.Status;

            if (Status == NozzleStatus.Ready)
            {
                foreach (Nozzle item in Nozzles)
                {
                    item.Status = NozzleStatus.Ready;
                }
            }

            switch (deviceResponse.Command)
            {
                case FuelDispenser.Commands.Command.Status:
                    break;
                case FuelDispenser.Commands.Command.StartFillingSum:
                    break;
                case FuelDispenser.Commands.Command.StartFillingQuantity:
                    break;
                case FuelDispenser.Commands.Command.StopFilling:
                    break;
                case FuelDispenser.Commands.Command.CompleteFilling:
                    break;
                case FuelDispenser.Commands.Command.ContinueFilling:
                    break;
                case FuelDispenser.Commands.Command.ChangePrice:
                    break;
                case FuelDispenser.Commands.Command.CounterLiter:
                    OnCounterReceived(deviceResponse);
                    break;
                case FuelDispenser.Commands.Command.CounterSum:
                    break;
                case FuelDispenser.Commands.Command.FirmwareVersion:
                    break;
                case FuelDispenser.Commands.Command.ProgramControlMode:
                    break;
                case FuelDispenser.Commands.Command.KeyboardControlMode:
                    break;
                case FuelDispenser.Commands.Command.Sensor:
                    break;
                case FuelDispenser.Commands.Command.ReduceCosts:
                    break;
                case FuelDispenser.Commands.Command.PumpAccelerationTime:
                    break;
                case FuelDispenser.Commands.Command.Screen:
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Обработчик события получения счетчика
        /// </summary>
        private void OnCounterReceived(ControllerResponse deviceResponse)
        {
            //Nozzle? nozzle = Nozzles.FirstOrDefault(n => n.ColumnId == columnId);

            //if (nozzle == null)
            //    return;

            //nozzle.LastCounter = quantity;

            //var shift = _shiftStore.CurrentShift;
            //if (shift == null || _shiftStore.CurrentShiftState is not (ShiftState.Open or ShiftState.Exceeded24Hours))
            //    return;

            //Task.Run(async () =>
            //{
            //    await CheckUncompletedSales();

            //    var shiftCounter = await _shiftCounterService.GetAsync(nozzle.Id, shift.Id);
            //    var totalSales = await _fuelSaleService.GetReceivedQuantityAsync(nozzle.Id, shift.Id);
            //    var unregisteredSales = await _unregisteredSaleService.GetAllAsync(nozzle.Id, shift.Id);

            //    if (shiftCounter == null)
            //        return;

            //    // Расчёт отклонения от начальных показаний
            //    decimal unregisteredSalesSum = unregisteredSales != null ? unregisteredSales.Sum(u => u.Quantity) : 0;
            //    decimal expectedCounter = shiftCounter.BeginSaleCounter + totalSales + unregisteredSalesSum;
            //    decimal unregisteredQuantity = nozzle.LastCounter - (shiftCounter.BeginNozzleCounter + expectedCounter);

            //    if (unregisteredQuantity != 0)
            //    {
            //        var unregisteredSale = new UnregisteredSale
            //        {
            //            NozzleId = nozzle.Id,
            //            ShiftId = shift.Id,
            //            CreateDate = DateTime.Now,
            //            State = UnregisteredSaleState.Waiting,
            //            Quantity = unregisteredQuantity,
            //            Sum = unregisteredQuantity * nozzle.Price
            //        };

            //        await _unregisteredSaleService.CreateAsync(unregisteredSale);
            //    }
            //});

        }

        /// <summary>
        /// Обработчик события опускания пистолета
        /// </summary>
        private void OnColumnLowered()
        {
            try
            {
                foreach (Nozzle nozzle in Nozzles)
                {
                    nozzle.Lifted = false;
                }
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// Обработчик события поднятия пистолета
        /// </summary>
        private void OnColumnLifted(Guid columnId)
        {
            //try
            //{
            //    foreach (Nozzle nozzle in Nozzles)
            //    {
            //        nozzle.Lifted = nozzle.ColumnId == columnId;
            //    }
            //}
            //catch (Exception)
            //{

            //}
        }

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
        /// Обработчик события создания продажи
        /// </summary>
        private void FuelSaleService_OnCreated(FuelSale fuelSale)
        {
            Nozzle? nozzle = Nozzles.FirstOrDefault(n => n.Id == fuelSale.NozzleId);

            if (nozzle == null)
            {
                return;
            }

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
                    //await _fuelDispenserService.SetPriceAsync(nozzle);
                    //if (fuelSale.IsForSum)
                    //{
                    //    await _fuelDispenserService.StartRefuelingSumAsync(nozzle);
                    //}
                    //else
                    //{
                    //    //await _fuelDispenserService.StartRefuelingQuantityAsync(nozzle);
                    //}
                }
            });

            //CanSelectedNozzle = false;
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

        /// <summary>
        /// При авторизации
        /// </summary>
        /// <param name="shift"></param>
        private void ShiftStore_OnLogin(Shift shift)
        {
            _cts = new CancellationTokenSource();
            _startTask = StartAsync(_cts.Token);
        }

        private async void UserStore_OnLogout()
        {
            await StopAsync();
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
