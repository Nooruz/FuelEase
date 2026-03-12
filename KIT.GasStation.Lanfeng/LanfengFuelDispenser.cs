//using KIT.GasStation.Domain.Models;
//using KIT.GasStation.FuelDispenser.Models;
//using KIT.GasStation.FuelDispenser.Services;
//using KIT.GasStation.HardwareConfigurations.Models;
//using KIT.GasStation.HardwareConfigurations.Services;
//using KIT.GasStation.Lanfeng.Utilities;
//using Microsoft.AspNetCore.SignalR.Client;
//using Serilog;
//using System.Diagnostics;
//using System.Text.RegularExpressions;

//namespace KIT.GasStation.Lanfeng
//{
//    /// <summary>
//    /// Сервис для работы с колонкой Lanfeng через COM-порт.
//    /// Прослушивает статусы и обрабатывает команды.
//    /// </summary>
//    public sealed class LanfengFuelDispenser : FuelDispenserServiceBase
//    {
//        #region Private Members

//        private readonly AsyncManualResetEvent _pauseGate = new(initialState: true);
//        private HubConnection _hub;
//        private volatile bool _pollingEnabled;
//        private Task _pollingTask;
//        private PortLease? _lease;
//        private readonly object _pollLock = new();
//        private const int _frameLen = 14;
//        private LanfengControllerType _controllerType;
//        private volatile bool _hardwareAvailable = true;
//        private string? _lastAvailabilityReason;
//        private bool _hubHandlersRegistered;
//        private int _hubRestartLoop;
//        private PortKey _portKey;
//        private CancellationToken _token;

//        #endregion

//        #region Constructors

//        //public LanfengFuelDispenser(Controller controller,
//        //    int address,
//        //    IHubClient hubClient,
//        //    ISharedSerialPortService sharedSerialPortService)
//        //    : base(controller, address, sharedSerialPortService, hubClient)
//        //{
//        //    _hubClient = hubClient;
//        //    _sharedSerialPortService = sharedSerialPortService;

//        //    if (Columns != null)
//        //    {
//        //        _controllerType = Columns.Count > 1 ? LanfengControllerType.Multi : LanfengControllerType.Single;
//        //    }

//        //    CreateLogger();
//        //}

//        #endregion

//        #region Protected Voids

//        protected override async Task OnOpenAsync(CancellationToken token)
//        {
//            try
//            {
//                _token = token;

//                _hub = _hubClient.Connection;
//                RegisterHubConnectionHandlers();

//                _logger.Information("Начало инициализации ТРК {Id}. Состояние HubConnection: {State}", Controller.Id, _hub?.State.ToString() ?? "null");

//                _logger.Debug("EnsureStartedAsync вызван. Текущее состояние: {State}", _hub.State);

//                _hub.On<StartPollingCommand>("StartPolling", async command =>
//                {
//                    await ExecuteHubCommandAsync(command.CommandId, command.GroupName,
//                        () => StartPollingAsync());
//                });

//                _hub.On<StopPollingCommand>("StopPolling", async command =>
//                {
//                    await ExecuteHubCommandAsync(command.CommandId, command.GroupName, StopPollingAsync);
//                });

//                _hub.On<Guid, Dictionary<string, decimal>>("SetPricesAsync", async (commandId, prices) =>
//                {
//                    var groupName = prices.Keys.FirstOrDefault() ?? string.Empty;
//                    await ExecuteHubCommandAsync(commandId, groupName, () => SetPricesAsync(prices));
//                });

//                _hub.On<Guid, string, decimal>("SetPriceAsync", async (commandId, groupName, price) =>
//                {
//                    await ExecuteHubCommandAsync(commandId, groupName,
//                        () => SetPriceAsync(groupName, price));
//                });

//                _hub.On<Guid, string>("InitializeConfigurationAsync", async (commandId, groupName) =>
//                {
//                    await ExecuteHubCommandAsync(commandId, groupName,
//                        () => InitializeConfigurationAsync(groupName));
//                });

//                _hub.On<string, decimal, bool>("StartFuelingAsync", async (groupName, sum, bySum) =>
//                {
//                    await StartFuelingAsync(groupName, sum, bySum);
//                });

//                _hub.On<string>("CompleteFuelingAsync", async (groupName) =>
//                {
//                    await CompleteFuelingAsync(groupName);
//                });

//                _hub.On<string, bool>("ChangeControlModeAsync", async (groupName, isProgramMode) =>
//                {
//                    await ChangeControlModeAsync(groupName, isProgramMode);
//                });

//                _hub.On<string>("StopFuelingAsync", async (groupName) =>
//                {
//                    await StopFuelingAsync(groupName);
//                });

//                _hub.On<string>("ResumeFuelingAsync", async (groupName) =>
//                {
//                    await ResumeFuelingAsync(groupName);
//                });

//                _hub.On<string>("GetStatusByAddressAsync", async (groupName) =>
//                {
//                    await GetStatusByAddressAsync(groupName);
//                });

//                _hub.On<Guid, string>("GetCounterAsync", async (commandId, groupName) =>
//                {
//                    await ExecuteHubCommandAsync(commandId, groupName, () => GetCounterAsync(groupName));
//                });

//                _hub.On<Guid, string>("GetCountersAsync", async (commandId, groupName) =>
//                {
//                    await ExecuteHubCommandAsync(commandId, groupName, () => GetCountersAsync());
//                });

//                // 1. СНАЧАЛА запускаем SignalR (синхронно)
//                await _hubClient.EnsureStartedAsync(token);

//                // 3. Присоединяемся к группам
//                await JoinWorkerGroupsAsync();
//                await BroadcastWorkerAvailabilityAsync(_hardwareAvailable, _lastAvailabilityReason, force: true);
//            }
//            catch (Exception e)
//            {
//                _logger.Error(e, "Ошибка в OnOpenAsync: {Message}", e.Message);
//                throw;
//            }
//        }

//        /// <summary>
//        /// Цикл опроса статуса ТРК.
//        /// Выполняется пока не придёт сигнал отмены.
//        /// </summary>
//        protected override async Task OnTickAsync()
//        {
//            _logger.Information("ТРК Lanfeng запущена, используется порт {Port}", Controller.ComPort);

//            // Даем время на выполнение инициализационных команд
//            await Task.Delay(3000, _token);

//            while (!_token.IsCancellationRequested && _pollingEnabled)
//            {
//                try
//                {
//                    // Ждем, если опрос приостановлен
//                    _pollingResumedEvent.Wait(_token);

//                    await ExecuteCommandSafeAsync(() =>
//                        ExecuteCommandAsync(Command.Status, Address, 0, null));
//                }
//                catch (OperationCanceledException ex) when (ex.CancellationToken == _token)
//                {
//                    _logger.Information("Опрос ТРК Lanfeng отменён: {Message}", ex.Message);
//                    break;
//                }
//                catch (Exception e)
//                {
//                    _logger.Error(e, e.Message, e.StackTrace);
//                    await Task.Delay(1000, _token);
//                }
//                if (!_pollingEnabled) break;
//            }
//        }

//        protected override async Task OnCloseAsync()
//        {
//            // Отписываемся от событий хаба
//            if (_hubHandlersRegistered && _hub != null)
//            {
//                _hub.Reconnecting -= OnHubReconnecting;
//                _hub.Reconnected -= OnHubReconnected;
//                _hub.Closed -= OnHubClosed;
//                _hubHandlersRegistered = false;
//            }

//            // Останавливаем опрос
//            if (_pollingEnabled)
//            {
//                await StopPollingAsync();
//            }

//            // Освобождаем порт
//            if (_lease != null)
//            {
//                await _lease.DisposeAsync();
//                _lease = null;
//            }

//            await base.OnCloseAsync();
//        }

//        public override async ValueTask DisposeAsync()
//        {
//            (_logger as IDisposable)?.Dispose();
//            await base.DisposeAsync();
//        }

//        #endregion

//        #region Private Voids

//        private async Task PublishStatusAsync(LanfengResonse resonse)
//        {
//            var column = GetColumnByAddress(resonse.StatusAddress);

//            if (column == null) return;

//            var parsed = new StatusResponse
//            {
//                Status = resonse.Status,
//                GroupName = column.GroupName
//            };

//            await _hub.InvokeAsync("PublishStatus", parsed, cancellationToken: _token);
//        }

//        private async Task PublishPumpWorkingAsync(LanfengResonse resonse)
//        {
//            var column = GetColumnByAddress(resonse.StatusAddress);

//            column ??= GetColumnByAddress(resonse.Address);

//            if (column == null) return;

//            var fuelingResponse = new FuelingResponse
//            {
//                GroupName = column.GroupName,
//                Quantity = resonse.Quantity,
//                Sum = resonse.Sum,
//            };

//            await _hub.InvokeAsync("OnFuelingAsync", fuelingResponse);
//        }

//        private async Task PublishWaitingStopAsync(LanfengResonse resonse)
//        {
//            var column = GetColumnByAddress(resonse.StatusAddress);

//            if (column == null) return;

//            await _hub.InvokeAsync("OnWaitingAsync", column.GroupName);
//        }

//        private async Task PublishPumpStopAsync(LanfengResonse resonse)
//        {
//            var column = GetColumnByAddress(resonse.StatusAddress);

//            if (column is null) return;

//            var fuelingResponse = new FuelingResponse
//            {
//                GroupName = column.GroupName,
//                Quantity = resonse.Quantity,
//                Sum = resonse.Sum,
//            };

//            await _hub.InvokeAsync("OnPumpStopAsync", fuelingResponse, _token);

//            await _hub.InvokeAsync("OnCompletedFuelingAsync", column.GroupName, fuelingResponse.Quantity, cancellationToken: _token);
//        }

//        private async Task PublishCounterLiterAsync(LanfengResonse resonse)
//        {
//            var column = GetColumnByAddress(resonse.Address);

//            if (column == null) return;

//            var counter = new CounterData
//            {
//                GroupName = column.GroupName,
//                Counter = resonse.CounterQuantity
//            };

//            await _hub.InvokeAsync("OnCounterUpdated", counter, cancellationToken: _token);
//        }

//        private async Task PublishCompleteFuelingAsync(LanfengResonse resonse)
//        {
//            var column = GetColumnByAddress(resonse.Address);

//            if (column == null) return;

//            await _hub.InvokeAsync("OnCompletedFuelingAsync", column.GroupName, resonse.Quantity, cancellationToken: _token);
//        }

//        private async Task ExecuteCommandAsync(
//            Command cmd,
//            int controllerAddress,
//            int nozzleMask,
//            decimal? value = null,
//            int expectedLength = _frameLen,
//            int writeToReadDelayMs = 100,
//            int readTimeoutMs = 3000,
//            int maxRetries = 2)
//        {
//            if (_sharedSerialPortService is null)
//                throw new InvalidOperationException("Последовательный порт еще не получен.");

//            await _pauseGate.WaitAsync(_token);     // уважаем паузу

//            try
//            {
//                var frame = ProtocolParser.BuildRequest(
//                    cmd,
//                    controllerAddress,
//                    columnAddress: nozzleMask,
//                    value: value,
//                    controllerType: _controllerType);

//                var response = new LanfengResonse();

//                byte[] rx;
//                var attempt = 0;

//                while (true)
//                {
//                    _token.ThrowIfCancellationRequested();
//                    attempt++;

//                    try
//                    {
//                        _logger.Information("[Tx] {Tx} экземпляр: {sd}", BitConverter.ToString(frame), GetHashCode());

//                        rx = await _sharedSerialPortService.WriteReadAsync(
//                            frame,
//                            expectedRxLength: expectedLength,
//                            writeToReadDelayMs: writeToReadDelayMs,
//                            readTimeoutMs: readTimeoutMs,
//                            maxRetries: maxRetries,
//                            ct: _token);
//                        await BroadcastWorkerAvailabilityAsync(true);

//                        response = ProtocolParser.ParseResponse(rx);

//                        if (response.IsValid)
//                        {
//                            _logger.Information("[Rx] {Rx}", BitConverter.ToString(response.Data));
//                            await BroadcastWorkerAvailabilityAsync(true);
//                            break; // всё ок, выходим из цикла
//                        }

//#if DEBUG
//                        _logger.Warning("Невалидный ответ от ТРК. Попытка {Attempt}. Повтор...", attempt);
//#endif
//                    }
//                    catch (Exception ex) when (IsCriticalSerialException(ex))
//                    {
//                        _logger.Error(ex, "Ошибка обмена с COM-портом, колонка будет отмечена как недоступная");
//                        await BroadcastWorkerAvailabilityAsync(false, ex.Message);
//                        throw;
//                    }
//                }

//                Column? column = null;

//                if (response.Command is Command.CounterLiter)
//                {
//                    if (_controllerType == LanfengControllerType.Single)
//                    {
//                        column = Columns.FirstOrDefault();
//                    }
//                    else
//                    {
//                        column = GetColumnByAddress(response.Address);
//                    }
//                }
//                else
//                {
//                    if (_controllerType == LanfengControllerType.Single)
//                    {
//                        column = Columns.FirstOrDefault();
//                    }
//                    else
//                    {
//                        column = GetColumnByAddress(response.StatusAddress);
//                    }
//                }

//                switch (response.Status)
//                {
//                    case NozzleStatus.PumpWorking:
//                        await PublishPumpWorkingAsync(response);
//                        break;
//                    case NozzleStatus.WaitingStop:
//                        await PublishWaitingStopAsync(response);
//                        await StopFuelingAsync(response);
//                        break;
//                    case NozzleStatus.PumpStop:
//                        await PublishPumpStopAsync(response);
//                        break;
//                    case NozzleStatus.WaitingRemoved:
//                        break;
//                    case NozzleStatus.Blocking:
//                        break;
//                    default:
//                        break;
//                }

//                switch (response.Command)
//                {
//                    case Command.Status:
//                        await PublishStatusAsync(response);
//                        break;
//                    case Command.StartFuelingSum:
//                        break;
//                    case Command.StartFuelingQuantity:
//                        break;
//                    case Command.StopFueling:
//                        break;
//                    case Command.CompleteFueling:
//                        await PublishCompleteFuelingAsync(response);
//                        break;
//                    case Command.ContinueFueling:
//                        break;
//                    case Command.ChangePrice:
//                        break;
//                    case Command.CounterLiter:
//                        await PublishCounterLiterAsync(response);
//                        break;
//                    case Command.CounterSum:
//                        break;
//                    case Command.FirmwareVersion:
//                        break;
//                    case Command.ProgramControlMode:
//                        break;
//                    case Command.KeyboardControlMode:
//                        break;
//                    default:
//                        break;
//                }

//                await HandleColumnLiftedAsync(response);
//            }
//            catch (Exception e)
//            {
//                _logger.Error(e, "Ошибка ExecuteCommandAsync: {Message}", e.Message);
//                throw;
//            }
//        }

//        private async Task ChangeControlModeAsync(string groupName, bool isProgramMode)
//        {
//            try
//            {
//                await PausePollingAsync();

//                var column = GetColumnByGroupName(groupName);
//                if (column is null)
//                {
//                    _logger.Warning("Колонка {GroupName} не найдена", groupName);
//                    return;
//                }

//                var cmd = isProgramMode ? Command.ProgramControlMode : Command.KeyboardControlMode;
//                await ExecuteCommandAsync(cmd, Address, column.LanfengAddress);
//            }
//            catch (Exception e)
//            {
//                _logger.Error(e, e.Message);
//            }
//            finally
//            {
//                await ResumePollingAsync();
//            }
//        }

//        private async Task SetPricesAsync(Dictionary<string, decimal> prices)
//        {
//            try
//            {
//                await PausePollingAsync();

//                foreach (var (group, price) in prices)
//                {
//                    var column = GetColumnByGroupName(group);

//                    if (column is null)
//                    {
//                        _logger.Warning("Колонка {GroupName} не найдена", group);
//                        return;
//                    }

//                    await ExecuteCommandAsync(Command.ChangePrice, Address, column.LanfengAddress, price);
//                }
//            }
//            catch (Exception e)
//            {
//                _logger.Error(e, e.Message);
//            }
//            finally
//            {
//                await ResumePollingAsync();
//            }
//        }

//        private async Task SetPriceAsync(string groupName, decimal price)
//        {
//            try
//            {
//                await PausePollingAsync();

//                var column = GetColumnByGroupName(groupName);

//                if (column is null)
//                {
//                    _logger.Warning("Колонка {GroupName} не найдена", groupName);
//                    return;
//                }

//                await ExecuteCommandAsync(Command.ChangePrice, Address, column.LanfengAddress, price);
//            }
//            catch (Exception e)
//            {
//                _logger.Error(e, e.Message);
//            }
//            finally
//            {
//                await ResumePollingAsync();
//            }
//        }

//        private async Task GetCounterAsync(string groupName)
//        {
//            try
//            {
//                await PausePollingAsync();

//                var column = Columns.FirstOrDefault(c => c.GroupName == groupName);

//                if (column == null) return;

//                await ExecuteCommandAsync(Command.CounterLiter, Address, column.LanfengAddress);
//            }
//            finally
//            {
//                await ResumePollingAsync();
//            }
//        }

//        private async Task GetCountersAsync()
//        {
//            try
//            {
//                await PausePollingAsync();

//                foreach (var item in Columns)
//                {
//                    await ExecuteCommandAsync(Command.CounterLiter, Address, item.LanfengAddress);
//                }
//            }
//            finally
//            {
//                await ResumePollingAsync();
//            }
//        }

//        private async Task InitializeConfigurationAsync(string groupName)
//        {
//            try
//            {
//                await PausePollingAsync();

//                await ExecuteCommandAsync(Command.Status, Address, 0);
//                await ExecuteCommandAsync(Command.FirmwareVersion, Address, 0);

//            }
//            finally
//            {
//                await ResumePollingAsync();
//            }
//        }

//        private async Task StartFuelingAsync(string groupName, decimal sum, bool bySum)
//        {
//            try
//            {
//                await PausePollingAsync();

//                var column = GetColumnByGroupName(groupName);

//                if (column is null)
//                {
//                    _logger.Warning("Колонка {GroupName} не найдена", groupName);
//                    return;
//                }

//                var cmd = bySum ? Command.StartFuelingSum : Command.StartFuelingQuantity;
//                await ExecuteCommandAsync(cmd, Address, column.LanfengAddress, sum);
//            }
//            catch (Exception e)
//            {
//                _logger.Error(e, e.Message);
//            }
//            finally
//            {
//                await ResumePollingAsync();
//            }
//        }

//        private async Task StopFuelingAsync(string groupName)
//        {
//            try
//            {
//                await PausePollingAsync();

//                var column = GetColumnByGroupName(groupName);

//                if (column is null)
//                {
//                    _logger.Warning("Колонка {GroupName} не найдена", groupName);
//                    return;
//                }

//                await ExecuteCommandAsync(Command.StopFueling, Address, column.LanfengAddress);
//            }
//            catch (Exception e)
//            {
//                _logger.Error(e, e.Message);
//            }
//            finally
//            {
//                await ResumePollingAsync();
//            }
//        }

//        private async Task StopFuelingAsync(LanfengResonse resonse)
//        {
//            try
//            {
//                var column = GetColumnByAddress(resonse.StatusAddress);

//                if (column is null) return;

//                var frame = ProtocolParser.BuildRequest(
//                    Command.CompleteFueling,
//                    Address,
//                    columnAddress: column.LanfengAddress,
//                    controllerType: _controllerType);

//                _logger.Information("[Tx] {Tx} экземпляр: {sd}", BitConverter.ToString(frame), GetHashCode());

//                var rx = await _sharedSerialPortService.WriteReadAsync(
//                            frame,
//                            expectedRxLength: _frameLen,
//                            writeToReadDelayMs: 100,
//                            readTimeoutMs: 3000,
//                            maxRetries: 2,
//                            ct: _token);
//            }
//            catch (Exception e)
//            {
//                _logger.Error(e, e.Message);
//            }
//        }

//        private async Task ResumeFuelingAsync(string groupName)
//        {
//            try
//            {
//                var column = GetColumnByGroupName(groupName);

//                if (column is null)
//                {
//                    _logger.Warning("Колонка {GroupName} не найдена", groupName);
//                    return;
//                }
//                await ExecuteCommandAsync(Command.ContinueFueling, Address, column.LanfengAddress);
//            }
//            catch (Exception e)
//            {
//                _logger.Error(e, e.Message);
//            }
//        }

//        private async Task GetStatusByAddressAsync(string groupName)
//        {
//            try
//            {
//                await PausePollingAsync();

//                var column = GetColumnByGroupName(groupName);

//                if (column is null)
//                {
//                    _logger.Warning("Колонка {GroupName} не найдена", groupName);
//                    return;
//                }
//                await ExecuteCommandAsync(Command.Status, Address, column.LanfengAddress);
//            }
//            catch (Exception e)
//            {
//                _logger.Error(e, e.Message);
//            }
//            finally
//            {
//                await ResumePollingAsync();
//            }
//        }

//        /// <summary>
//        /// Метод остановки цикла опроса.
//        /// </summary>
//        private async Task StopPollingAsync()
//        {
//            Task? toAwait = null;
//            lock (_pollLock)
//            {
//                if (!_pollingEnabled) return;     // уже остановлено
//                _pollingEnabled = false;
//                toAwait = _pollingTask;
//            }

//            // даём циклу корреткно выйти
//            if (toAwait is not null)
//            {
//                try { await toAwait; } catch { /* игнор логика выхода */ }
//            }

//            // освобождаем lease и физически закрываем COM, если больше никто не держит
//            if (_lease is not null)
//            {
//                await _lease.DisposeAsync();
//                _lease = null;
//            }
//            _sharedSerialPortService = null!;
//        }

//        /// <summary>
//        /// Метод запуска цикла опроса.
//        /// </summary>
//        private async Task StartPollingAsync()
//        {
//            if (_pollingTask == null || _pollingTask.IsCompleted)
//            {
//                lock (_pollLock)
//                {
//                    if (_pollingEnabled) return;      // уже запущено
//                }

//                try
//                {
//                    // 2) Только после успешного Acquire включаем флаг
//                    lock (_pollLock)
//                    {
//                        _pollingEnabled = true;
//                    }

//                    // 3) Стартуем цикл опроса (lease остаётся жить в поле)
//                    _pollingTask = OnTickAsync();
//                    await BroadcastWorkerAvailabilityAsync(true, "Polling started");
//                }
//                catch (Exception ex)
//                {
//                    _logger.Error(ex, "Не удалось стартовать polling");
//                    lock (_pollLock) { _pollingEnabled = false; }

//                    // если успели получить лизу — аккуратно отпустим и обнулим
//                    if (_lease is not null)
//                    {
//                        await _lease.DisposeAsync();
//                        _lease = null;
//                    }
//                    _sharedSerialPortService = null!;
//                    await BroadcastWorkerAvailabilityAsync(false);
//                }
//            }
//        }

//        private async Task HandleColumnLiftedAsync(LanfengResonse response)
//        {
//            if (response.Data is null) return;

//            // Протокол: в младшем полубайте (low nibble) порядковый номер пистолета (1..n).
//            int liftedOrdinal = response.Data[12] & 0x0F;

//            if (liftedOrdinal == 0)
//            {
//                var liftedColumn = Columns.FirstOrDefault(c => c.IsLifted);
//                if (liftedColumn is not null)
//                {
//                    liftedColumn.IsLifted = false;
//                    await _hub.InvokeAsync("ColumnLiftedChanged", liftedColumn.GroupName, false);
//                }
//            }
//            else
//            {
//                var column = Columns.FirstOrDefault(c => c.LanfengAddress == liftedOrdinal);
//                if (column is not null)
//                {
//                    column.IsLifted = true;
//                    await _hub.InvokeAsync("ColumnLiftedChanged", column.GroupName, true);
//                }
//            }
//        }

//        private async Task CompleteFuelingAsync(string groupName)
//        {
//            try
//            {
//                await ResumePollingAsync();

//                await Task.Delay(300);

//                var column = GetColumnByGroupName(groupName);

//                if (column is null)
//                {
//                    _logger.Warning("Колонка {GroupName} не найдена", groupName);
//                    return;
//                }

//                await ExecuteCommandAsync(Command.CompleteFueling, Address, column.LanfengAddress);
//            }
//            catch (Exception e)
//            {
//                _logger.Error(e, e.Message);
//            }
//            finally
//            {
//                await ResumePollingAsync();
//            }
//        }

//        private Column? GetColumnByGroupName(string groupName)
//        {
//            return Columns.FirstOrDefault(c => c.GroupName == groupName);
//        }

//        private Column? GetColumnByAddress(int? address)
//        {
//            return Columns.FirstOrDefault(c => c.LanfengAddress == address);
//        }

//        #endregion

//        #region Hub

//        private async Task ExecuteHubCommandAsync(Guid commandId, string groupName, Func<Task> action)
//        {
//            Exception? error = null;
//            try
//            {
//                await action();
//            }
//            catch (Exception ex)
//            {
//                error = ex;
//                _logger.Error(ex, "Ошибка выполнения команды {CommandId} для {Group}", commandId, groupName);
//            }

//            await ReportCommandCompletedAsync(commandId, groupName, error);
//        }

//        private async Task ReportCommandCompletedAsync(Guid commandId, string groupName, Exception? error)
//        {
//            if (_hub is null || _hub.State != HubConnectionState.Connected)
//                return;

//            var completion = new CommandCompletion
//            {
//                CommandId = commandId,
//                GroupName = groupName,
//                IsSuccess = error is null,
//                ErrorMessage = error?.Message
//            };

//            try
//            {
//                await _hub.InvokeAsync("ReportCommandCompleted", completion);
//            }
//            catch (Exception ex)
//            {
//                _logger.Error(ex, "Не удалось отправить подтверждение команды {CommandId}", commandId);
//            }
//        }

//        private void RegisterHubConnectionHandlers()
//        {
//            if (_hubHandlersRegistered || _hub is null)
//                return;

//            _hub.Reconnecting += OnHubReconnecting;
//            _hub.Reconnected += OnHubReconnected;
//            _hub.Closed += OnHubClosed;
//            _hubHandlersRegistered = true;
//        }

//        private Task OnHubReconnecting(Exception? error)
//        {
//            _logger.Warning("Потеряно соединение с сервером: {Message}", error?.Message ?? "unknown");
//            return Task.CompletedTask;
//        }

//        private async Task OnHubReconnected(string? connectionId)
//        {
//            _logger.Information("Переподключен с сервером. ConnectionId={ConnectionId}", connectionId);
//            try
//            {
//                await JoinWorkerGroupsAsync();
//                await BroadcastWorkerAvailabilityAsync(_hardwareAvailable, _lastAvailabilityReason, force: true);
//            }
//            catch (Exception ex)
//            {
//                _logger.Error(ex, "Не удалось повторно присоединиться к группам после переподключения");
//            }
//        }

//        private Task OnHubClosed(Exception? error)
//        {
//            _logger.Error(error, "Соединение с SignalR было закрыто");
//            return RestartHubConnectionLoopAsync();
//        }

//        private Task RestartHubConnectionLoopAsync()
//        {
//            if (_hub is null)
//                return Task.CompletedTask;

//            if (Interlocked.CompareExchange(ref _hubRestartLoop, 1, 0) != 0)
//                return Task.CompletedTask;

//            return Task.Run(async () =>
//            {
//                try
//                {
//                    while (_hub.State != HubConnectionState.Connected)
//                    {
//                        try
//                        {
//                            await _hubClient.EnsureStartedAsync();
//                            await JoinWorkerGroupsAsync();
//                            await BroadcastWorkerAvailabilityAsync(_hardwareAvailable, _lastAvailabilityReason, force: true);
//                            break;
//                        }
//                        catch (Exception ex)
//                        {
//                            _logger.Error(ex, "Не удалось переподключиться к SignalR, повтор через 5 секунд");
//                            await Task.Delay(TimeSpan.FromSeconds(5));
//                        }
//                    }
//                }
//                finally
//                {
//                    Interlocked.Exchange(ref _hubRestartLoop, 0);
//                }
//            });
//        }

//        private async Task JoinWorkerGroupsAsync()
//        {
//            if (_hub is null || Controller?.Columns is null)
//                return;

//            // Ждем, пока соединение станет активным
//            var timeout = TimeSpan.FromSeconds(10);
//            var stopwatch = Stopwatch.StartNew();

//            while (_hub.State != HubConnectionState.Connected && stopwatch.Elapsed < timeout)
//            {
//                await Task.Delay(100);
//            }

//            if (_hub.State != HubConnectionState.Connected)
//                throw new InvalidOperationException("Не удалось подключиться к SignalR за отведенное время");

//            foreach (var item in Controller.Columns)
//            {
//                if (string.IsNullOrWhiteSpace(item.GroupName))
//                {
//                    item.GroupName = $"{Controller.Name}/{item.Name}";
//                }

//                await _hub.InvokeAsync("JoinController", item.GroupName, true);
//            }
//        }

//        private async Task BroadcastWorkerAvailabilityAsync(bool isAvailable, string? reason = null, bool force = false)
//        {
//            var sanitizedReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();

//            if (!force &&
//                _hardwareAvailable == isAvailable &&
//                string.Equals(_lastAvailabilityReason ?? string.Empty, sanitizedReason ?? string.Empty, StringComparison.Ordinal))
//            {
//                return;
//            }

//            _hardwareAvailable = isAvailable;
//            _lastAvailabilityReason = sanitizedReason;

//            if (_hub is null || _hub.State != HubConnectionState.Connected)
//                return;

//            if (Controller?.Columns is null)
//                return;

//            var groups = Controller.Columns
//                .Where(c => !string.IsNullOrWhiteSpace(c.GroupName))
//                .Select(c => c.GroupName!);

//            var tasks = groups.Select(group => SendAvailabilityAsync(group, isAvailable, sanitizedReason));
//            await Task.WhenAll(tasks);
//        }

//        private async Task SendAvailabilityAsync(string groupName, bool isAvailable, string? reason)
//        {
//            try
//            {
//                var report = new WorkerAvailabilityReport
//                {
//                    GroupName = groupName,
//                    IsAvailable = isAvailable,
//                    Reason = reason
//                };
//                await _hub.InvokeAsync("ReportWorkerAvailability", report);
//            }
//            catch (Exception ex)
//            {
//                _logger.Error(ex, "Не удалось отправить состояние worker для {Group}", groupName);
//            }
//        }

//        private static bool IsCriticalSerialException(Exception ex) =>
//            ex is TimeoutException || ex is IOException || ex is InvalidOperationException || ex is UnauthorizedAccessException;

//        #endregion

//        #region Пауза и продолжения опроса

//        private readonly ManualResetEventSlim _pollingResumedEvent = new(true);
//        private readonly SemaphoreSlim _commandGate = new(1, 1);

//        private async Task PausePollingAsync()
//        {
//            _pollingResumedEvent.Reset();

//            // ⛔ ждём, пока текущая команда ДОРАБОТАЕТ
//            await _commandGate.WaitAsync();
//            _commandGate.Release();
//        }

//        private async Task ResumePollingAsync()
//        {
//            _pollingResumedEvent.Set();
//            await Task.CompletedTask;
//        }

//        private async Task ExecuteCommandSafeAsync(
//            Func<Task> command)
//        {
//            await _commandGate.WaitAsync(_token);
//            try
//            {
//                await command(); // <-- вот тут команда гарантированно ДОРАБАТЫВАЕТ
//            }
//            finally
//            {
//                _commandGate.Release();
//            }
//        }

//        #endregion

//        #region Logs

//        private void CreateLogger()
//        {
//            // создаём общую папку для логов ТРК
//            var logRoot = Path.Combine(AppContext.BaseDirectory, "logs", "trk");
//            Directory.CreateDirectory(logRoot);

//            // безопасное имя файла (уникальное для экземпляра)
//            string safeController = Sanitize(Controller.Name);
//            string safePort = Sanitize(Controller.ComPort);
//            string controllerId = Controller.Id == Guid.Empty ? "noid" : Controller.Id.ToString("N");
//            string fileName = $"TRK_{Controller.Type}_{safeController}_{safePort}_{controllerId}_{Address}.log";
//            string path = Path.Combine(logRoot, fileName);

//            // отдельный Serilog для файла инстанса
//            _logger = new LoggerConfiguration()
//                .MinimumLevel.Debug()
//                .Enrich.WithProperty("Controller", Controller.Name)
//                .Enrich.WithProperty("Address", Address)
//                .Enrich.WithProperty("ComPort", Controller.ComPort)
//                .WriteTo.File(
//                    path: path,
//                    rollingInterval: RollingInterval.Day,
//                    retainedFileCountLimit: 14,
//                    shared: true,
//                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
//                )
//                .CreateLogger();

//            _logger.Information("Инициализация Lanfeng для {Controller}/{Address} на порту {Port}",
//            Controller.Name, Address, Controller.ComPort);
//        }

//        // sanitization для имени файла
//        private static string Sanitize(string s)
//        {
//            if (string.IsNullOrWhiteSpace(s)) return "UNNAMED";
//            s = s.Trim();
//            s = Regex.Replace(s, @"[^\w\-\.\(\) ]+", "_"); // заменяем недопустимые символы
//            return s.Length > 80 ? s[..80] : s;
//        }

//        #endregion
//    }
//}

