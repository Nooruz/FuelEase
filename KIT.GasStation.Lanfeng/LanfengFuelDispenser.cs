using KIT.GasStation.Domain.Models;
using KIT.GasStation.FuelDispenser;
using KIT.GasStation.FuelDispenser.Hubs;
using KIT.GasStation.FuelDispenser.Models;
using KIT.GasStation.HardwareConfigurations.Models;
using KIT.GasStation.HardwareConfigurations.Services;
using KIT.GasStation.Lanfeng.Utilities;
using Microsoft.AspNetCore.SignalR.Client;
using System.Diagnostics;
using System.IO.Ports;

namespace KIT.GasStation.Lanfeng
{
    /// <summary>
    /// Сервис управления ТРК Lanfeng через COM-порт по протоколу RS-485 (9600, N, 8, 1).
    /// </summary>
    public sealed class LanfengFuelDispenser : IFuelDispenserService
    {
        #region Private Members

        private readonly IHubClient _hubClient;
        private readonly IPortManager _portManager;
        private LanfengDeviceLogger? _deviceLogger;

        private HubConnection _hub;
        private ISharedSerialPortService _sharedSerialPortService;
        private PortLease? _lease;

        private volatile bool _pollingEnabled;
        private Task _pollingTask;
        private readonly object _pollLock = new();

        private LanfengControllerType _controllerType;
        private volatile bool _hardwareAvailable = true;
        private string? _lastAvailabilityReason;
        private bool _hubHandlersRegistered;
        private int _hubRestartLoop;
        private CancellationToken _token;

        private readonly ManualResetEventSlim _pollingResumedEvent = new(true);
        private readonly SemaphoreSlim _commandGate = new(1, 1);

        private const int FrameLen = 14;

        #endregion

        #region Public Properties

        public Controller Controller { get; set; }

        /// <summary>Адрес ТРК в RS-485 (берётся из первой колонки).</summary>
        private int ControllerAddress => Controller.Columns.FirstOrDefault()?.Address ?? 0;

        #endregion

        #region Constructors

        public LanfengFuelDispenser(
            IHubClient hubClient,
            IPortManager portManager)
        {
            _hubClient = hubClient;
            _portManager = portManager;
        }

        #endregion

        #region IFuelDispenserService

        public async Task RunAsync(CancellationToken token)
        {
            var opened = false;
            try
            {
                await OnOpenAsync(token);
                opened = true;

                // Ожидаем отмены; опрос идёт в фоновом _pollingTask
                await Task.Delay(Timeout.Infinite, token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                _deviceLogger?.Info("ТРК Lanfeng: токен отмены получен, завершаем работу");
            }
            finally
            {
                if (opened)
                    await OnCloseAsync();
            }
        }

        public ValueTask DisposeAsync()
        {
            _pollingResumedEvent.Dispose();
            _commandGate.Dispose();
            return ValueTask.CompletedTask;
        }

        #endregion

        #region IDeviceCommandClient

        public async Task SetPricesAsync(Guid commandId, IReadOnlyCollection<PriceRequest> prices)
        {
            try
            {
                await PausePollingAsync();
                foreach (var request in prices)
                {
                    var column = GetColumnByGroupName(request.GroupName);
                    if (column is null)
                    {
                        _deviceLogger?.Warning("Колонка {GroupName} не найдена", request.GroupName);
                        continue;
                    }
                    await ExecuteCommandAsync(Command.ChangePrice, column.LanfengAddress, request.Value);
                }
            }
            catch (Exception e)
            {
                _deviceLogger?.Error(e, e.Message);
            }
            finally
            {
                await ResumePollingAsync();
            }
        }

        public async Task SetPriceAsync(Guid commandId, PriceRequest priceRequest)
        {
            try
            {
                await PausePollingAsync();
                var column = GetColumnByGroupName(priceRequest.GroupName);
                if (column is null)
                {
                    _deviceLogger?.Warning("Колонка {GroupName} не найдена", priceRequest.GroupName);
                    return;
                }
                await ExecuteCommandAsync(Command.ChangePrice, column.LanfengAddress, priceRequest.Value);
            }
            catch (Exception e)
            {
                _deviceLogger?.Error(e, e.Message);
            }
            finally
            {
                await ResumePollingAsync();
            }
        }

        public async Task StartFuelingAsync(FuelingRequest fuelingRequest)
        {
            try
            {
                await PausePollingAsync();
                var column = GetColumnByGroupName(fuelingRequest.GroupName);
                if (column is null)
                {
                    _deviceLogger?.Warning("Колонка {GroupName} не найдена", fuelingRequest.GroupName);
                    return;
                }
                var cmd = fuelingRequest.FuelingStartMode == FuelingStartMode.ByAmount
                    ? Command.StartFuelingSum
                    : Command.StartFuelingQuantity;
                await ExecuteCommandAsync(cmd, column.LanfengAddress, fuelingRequest.Value);
            }
            catch (Exception e)
            {
                _deviceLogger?.Error(e, e.Message);
            }
            finally
            {
                await ResumePollingAsync();
            }
        }

        public async Task StopFuelingAsync(string groupName)
        {
            try
            {
                await PausePollingAsync();
                var column = GetColumnByGroupName(groupName);
                if (column is null)
                {
                    _deviceLogger?.Warning("Колонка {GroupName} не найдена", groupName);
                    return;
                }
                await ExecuteCommandAsync(Command.StopFueling, column.LanfengAddress);
            }
            catch (Exception e)
            {
                _deviceLogger?.Error(e, e.Message);
            }
            finally
            {
                await ResumePollingAsync();
            }
        }

        public async Task ResumeFuelingAsync(ResumeFuelingRequest resumeFuelingRequest)
        {
            try
            {
                await PausePollingAsync();
                var column = GetColumnByGroupName(resumeFuelingRequest.GroupName);
                if (column is null)
                {
                    _deviceLogger?.Warning("Колонка {GroupName} не найдена", resumeFuelingRequest.GroupName);
                    return;
                }
                await ExecuteCommandAsync(Command.ContinueFueling, column.LanfengAddress);
            }
            catch (Exception e)
            {
                _deviceLogger?.Error(e, e.Message);
            }
            finally
            {
                await ResumePollingAsync();
            }
        }

        public async Task GetStatusByAddressAsync(string groupName)
        {
            try
            {
                await PausePollingAsync();
                var column = GetColumnByGroupName(groupName);
                if (column is null)
                {
                    _deviceLogger?.Warning("Колонка {GroupName} не найдена", groupName);
                    return;
                }
                await ExecuteCommandAsync(Command.Status, column.LanfengAddress);
            }
            catch (Exception e)
            {
                _deviceLogger?.Error(e, e.Message);
            }
            finally
            {
                await ResumePollingAsync();
            }
        }

        public async Task CompleteFuelingAsync(string groupName)
        {
            try
            {
                await PausePollingAsync();
                await Task.Delay(300, _token);
                var column = GetColumnByGroupName(groupName);
                if (column is null)
                {
                    _deviceLogger?.Warning("Колонка {GroupName} не найдена", groupName);
                    return;
                }
                await ExecuteCommandAsync(Command.CompleteFueling, column.LanfengAddress);
            }
            catch (Exception e)
            {
                _deviceLogger?.Error(e, e.Message);
            }
            finally
            {
                await ResumePollingAsync();
            }
        }

        public async Task GetCounterAsync(Guid commandId, string groupName)
        {
            try
            {
                await PausePollingAsync();
                var column = GetColumnByGroupName(groupName);
                if (column is null) return;
                await ExecuteCommandAsync(Command.CounterLiter, column.LanfengAddress);
            }
            finally
            {
                await ResumePollingAsync();
            }
        }

        public async Task GetCountersAsync(Guid commandId, string groupName)
        {
            try
            {
                await PausePollingAsync();
                foreach (var column in Controller.Columns)
                    await ExecuteCommandAsync(Command.CounterLiter, column.LanfengAddress);
            }
            finally
            {
                await ResumePollingAsync();
            }
        }

        public async Task ChangeControlModeAsync(Guid commandId, string groupName, bool isProgramMode)
        {
            try
            {
                await PausePollingAsync();
                var column = GetColumnByGroupName(groupName);
                if (column is null)
                {
                    _deviceLogger?.Warning("Колонка {GroupName} не найдена", groupName);
                    return;
                }
                var cmd = isProgramMode ? Command.ProgramControlMode : Command.KeyboardControlMode;
                await ExecuteCommandAsync(cmd, column.LanfengAddress);
            }
            catch (Exception e)
            {
                _deviceLogger?.Error(e, e.Message);
            }
            finally
            {
                await ResumePollingAsync();
            }
        }

        public async Task InitializeConfigurationAsync(Guid commandId, string groupName)
        {
            try
            {
                await PausePollingAsync();

                // Переключаем каждую колонку в программный режим управления.
                await ExecuteCommandAsync(Command.ProgramControlMode, 0);

                // Запрашиваем статус и версию прошивки
                await ExecuteCommandAsync(Command.Status, 0);
                await ExecuteCommandAsync(Command.FirmwareVersion, 0);
            }
            finally
            {
                await ResumePollingAsync();
            }
        }

        #endregion

        #region Lifecycle

        private async Task OnOpenAsync(CancellationToken token)
        {
            try
            {
                _token = token;
                _deviceLogger = new LanfengDeviceLogger(Controller.ComPort, ControllerAddress);
                _hub = _hubClient.Connection;
                RegisterHubConnectionHandlers();

                _deviceLogger?.Info("Инициализация ТРК Lanfeng {Id} на порту {Port}",
                    Controller.Id, Controller.ComPort);

                // Открываем COM-порт: 9600, N, 8, 1
                var key = new PortKey(
                    portName: Controller.ComPort,
                    baudRate: Controller.BaudRate,
                    parity: Parity.None,
                    dataBits: 8,
                    stopBits: StopBits.One);

                var options = new SerialPortOptions(
                    RtsEnable: false,
                    DtrEnable: false,
                    ReadTimeoutMs: 3000,
                    WriteTimeoutMs: 3000,
                    ReadBufferSize: 1024,
                    WriteBufferSize: 1024);

                _lease = await _portManager.AcquireAsync(key, options, token);
                _sharedSerialPortService = _lease.Port;

                _controllerType = Controller.Columns.Count > 1
                    ? LanfengControllerType.Multi
                    : LanfengControllerType.Single;

                // Подключаемся к SignalR и присоединяемся к группам
                await _hubClient.EnsureStartedAsync(token);
                await JoinWorkerGroupsAsync();
                await BroadcastWorkerAvailabilityAsync(_hardwareAvailable, _lastAvailabilityReason, force: true);

                // Запускаем цикл опроса
                await StartPollingAsync();
            }
            catch (Exception e)
            {
                _deviceLogger?.Error(e, "Ошибка в OnOpenAsync: {Message}", e.Message);
                throw;
            }
        }

        private async Task OnCloseAsync()
        {
            if (_hubHandlersRegistered && _hub != null)
            {
                _hub.Reconnecting -= OnHubReconnecting;
                _hub.Reconnected -= OnHubReconnected;
                _hub.Closed -= OnHubClosed;
                _hubHandlersRegistered = false;
            }

            await StopPollingAsync();

            _deviceLogger?.Dispose();
            _deviceLogger = null;
        }

        #endregion

        #region Polling

        private async Task StartPollingAsync()
        {
            lock (_pollLock)
            {
                if (_pollingEnabled) return;
                _pollingEnabled = true;
            }

            _pollingTask = PollingLoopAsync();
            await BroadcastWorkerAvailabilityAsync(true, "Polling started");
        }

        private async Task StopPollingAsync()
        {
            Task? toAwait = null;
            lock (_pollLock)
            {
                if (!_pollingEnabled) return;
                _pollingEnabled = false;
                toAwait = _pollingTask;
            }

            // Разблокируем цикл, если он завис на паузе
            _pollingResumedEvent.Set();

            if (toAwait != null)
            {
                try { await toAwait; } catch { /* штатное завершение */ }
            }

            if (_lease != null)
            {
                await _lease.DisposeAsync();
                _lease = null;
            }
            _sharedSerialPortService = null!;
        }

        private async Task PollingLoopAsync()
        {
            _deviceLogger?.Info("ТРК Lanfeng: опрос запущен, порт {Port}", Controller.ComPort);

            // Небольшая пауза перед первым запросом
            await Task.Delay(3000, _token).ConfigureAwait(false);

            while (!_token.IsCancellationRequested && _pollingEnabled)
            {
                try
                {
                    _pollingResumedEvent.Wait(_token);

                    await ExecuteCommandSafeAsync(() =>
                        ExecuteCommandAsync(Command.Status, 0));
                }
                catch (OperationCanceledException ex) when (ex.CancellationToken == _token)
                {
                    _deviceLogger?.Info("Опрос ТРК Lanfeng отменён");
                    break;
                }
                catch (Exception e)
                {
                    _deviceLogger?.Error(e, "Ошибка в цикле опроса: {Message}", e.Message);
                    await Task.Delay(1000, _token).ConfigureAwait(false);
                }

                if (!_pollingEnabled) break;
            }

            _deviceLogger?.Info("ТРК Lanfeng: опрос завершён");
        }

        #endregion

        #region Protocol

        private async Task ExecuteCommandAsync(
            Command cmd,
            int nozzleMask,
            decimal? value = null,
            int writeToReadDelayMs = 200,
            int readTimeoutMs = 3000,
            int maxRetries = 2)
        {
            if (_sharedSerialPortService is null)
                throw new InvalidOperationException("COM-порт не инициализирован.");

            var frame = ProtocolParser.BuildRequest(
                cmd,
                ControllerAddress,
                columnAddress: nozzleMask,
                value: value,
                controllerType: _controllerType);

            _deviceLogger?.LogTx(frame);

            byte[] rx;
            try
            {
                rx = await _sharedSerialPortService.WriteReadAsync(
                    frame,
                    expectedRxLength: FrameLen,
                    writeToReadDelayMs: writeToReadDelayMs,
                    readTimeoutMs: readTimeoutMs,
                    maxRetries: maxRetries,
                    ct: _token);

                await BroadcastWorkerAvailabilityAsync(true);
            }
            catch (Exception ex) when (IsCriticalSerialException(ex))
            {
                _deviceLogger?.Error(ex, "Ошибка обмена с COM-портом, ТРК отмечена как недоступная");
                await BroadcastWorkerAvailabilityAsync(false, ex.Message);
                throw;
            }

            var response = ProtocolParser.ParseResponse(rx);

            if (!response.IsValid)
            {
                _deviceLogger?.Warning("Невалидный ответ от ТРК Lanfeng (команда {Cmd})", cmd);
                return;
            }

            _deviceLogger?.LogRx(rx);

            await HandleResponseAsync(response);
        }

        private async Task HandleResponseAsync(LanfengResonse response)
        {
            // Обрабатываем статус пистолета
            switch (response.Status)
            {
                case NozzleStatus.PumpWorking:
                    await PublishPumpWorkingAsync(response);
                    break;
                case NozzleStatus.WaitingStop:
                    await PublishWaitingStopAsync(response);
                    await SendAutoCompleteFrameAsync(response);
                    break;
                case NozzleStatus.PumpStop:
                    await PublishPumpStopAsync(response);
                    break;
            }

            // Обрабатываем тип команды в ответе
            switch (response.Command)
            {
                case Command.Status:
                    await PublishStatusAsync(response);
                    await HandleColumnLiftedAsync(response);
                    break;
                case Command.CompleteFueling:
                    await PublishCompleteFuelingAsync(response);
                    break;
                case Command.CounterLiter:
                    await PublishCounterLiterAsync(response);
                    break;
            }
        }

        /// <summary>
        /// Автоматически отправляет CompleteFueling при WaitingStop (пистолет повешен).
        /// </summary>
        private async Task SendAutoCompleteFrameAsync(LanfengResonse response)
        {
            try
            {
                var column = ResolveColumn(response.StatusAddress);
                if (column is null) return;

                var frame = ProtocolParser.BuildRequest(
                    Command.CompleteFueling,
                    ControllerAddress,
                    columnAddress: column.LanfengAddress,
                    controllerType: _controllerType);

                _deviceLogger?.LogTx(frame);

                await _sharedSerialPortService.WriteReadAsync(
                    frame,
                    expectedRxLength: FrameLen,
                    writeToReadDelayMs: 100,
                    readTimeoutMs: 3000,
                    maxRetries: 2,
                    ct: _token);
            }
            catch (Exception e)
            {
                _deviceLogger?.Error(e, "Ошибка SendAutoCompleteFrameAsync: {Message}", e.Message);
            }
        }

        #endregion

        #region Publish

        private async Task PublishStatusAsync(LanfengResonse response)
        {
            var column = ResolveColumn(response.StatusAddress);
            if (column is null) return;

            await _hub.InvokeAsync("PublishStatus",
                new StatusResponse { GroupName = column.GroupName, Status = response.Status },
                cancellationToken: _token);
        }

        private async Task PublishPumpWorkingAsync(LanfengResonse response)
        {
            var column = ResolveColumn(response.StatusAddress) ?? ResolveColumn(response.Address);
            if (column is null) return;

            await _hub.InvokeAsync("FuelingAsync",
                new FuelingResponse { GroupName = column.GroupName, Quantity = response.Quantity, Sum = response.Sum },
                cancellationToken: _token);
        }

        private async Task PublishWaitingStopAsync(LanfengResonse response)
        {
            var column = ResolveColumn(response.StatusAddress);
            if (column is null) return;

            await _hub.InvokeAsync("WaitingAsync", column.GroupName, cancellationToken: _token);
        }

        private async Task PublishPumpStopAsync(LanfengResonse response)
        {
            var column = ResolveColumn(response.StatusAddress);
            if (column is null) return;

            var fuelingResponse = new FuelingResponse
            {
                GroupName = column.GroupName,
                Quantity = response.Quantity,
                Sum = response.Sum
            };

            await _hub.InvokeAsync("PumpStopAsync", fuelingResponse, cancellationToken: _token);
            await CompleteFuelingAsync(column);
        }



        private async Task PublishCounterLiterAsync(LanfengResonse response)
        {
            var column = ResolveColumn(response.Address);
            if (column is null) return;

            await _hub.InvokeAsync("CounterUpdated",
                new CounterData { GroupName = column.GroupName, Counter = response.CounterQuantity },
                cancellationToken: _token);
        }

        private async Task PublishCompleteFuelingAsync(LanfengResonse response)
        {
            var column = ResolveColumn(response.Address);
            if (column is null) return;

            await _hub.InvokeAsync("CompletedFuelingAsync", column.GroupName, response.Quantity, cancellationToken: _token);
        }

        /// <summary>
        /// Обрабатывает байт 13 (индекс 12) ответа на команду Status:
        ///   0x00 — ни один пистолет не поднят;
        ///   0x01 — поднят первый пистолет, 0x02 — второй и т.д. (порядковый номер).
        /// Для однорукавного контроллера любое ненулевое значение означает подъём единственного пистолета.
        /// Для многорукавного — значение соответствует порядковому номеру пистолета (Nozzle).
        /// </summary>
        private async Task HandleColumnLiftedAsync(LanfengResonse response)
        {
            if (response.Data is null || response.Data.Length < 13) return;

            int liftedNozzleNo = response.Data[12] & 0x0F; // 0 = не поднят, 1 = 1-й пистолет, 2 = 2-й, ...

            if (liftedNozzleNo == 0)
            {
                var liftedColumn = Controller.Columns.FirstOrDefault(c => c.IsLifted);
                if (liftedColumn is not null)
                {
                    liftedColumn.IsLifted = false;
                    await _hub.InvokeAsync("ColumnLiftedChanged", liftedColumn.GroupName, false, cancellationToken: _token);
                }
            }
            else
            {
                var column = Controller.Columns.FirstOrDefault(c => c.LanfengAddress == liftedNozzleNo);
                if (column is not null)
                {
                    column.IsLifted = true;
                    await _hub.InvokeAsync("ColumnLiftedChanged", column.GroupName, true, cancellationToken: _token);
                }
            }
        }

        #endregion

        #region Hub

        private void RegisterHubConnectionHandlers()
        {
            if (_hubHandlersRegistered || _hub is null) return;

            _hub.Reconnecting += OnHubReconnecting;
            _hub.Reconnected += OnHubReconnected;
            _hub.Closed += OnHubClosed;
            _hubHandlersRegistered = true;
        }

        private Task OnHubReconnecting(Exception? error)
        {
            _deviceLogger?.Warning("Потеряно соединение с SignalR: {Message}", error?.Message ?? "unknown");
            return Task.CompletedTask;
        }

        private async Task OnHubReconnected(string? connectionId)
        {
            _deviceLogger?.Info("SignalR переподключен. ConnectionId={ConnectionId}", connectionId);
            try
            {
                await JoinWorkerGroupsAsync();
                await BroadcastWorkerAvailabilityAsync(_hardwareAvailable, _lastAvailabilityReason, force: true);
            }
            catch (Exception ex)
            {
                _deviceLogger?.Error(ex, "Ошибка повторного присоединения к группам после переподключения");
            }
        }

        private Task OnHubClosed(Exception? error)
        {
            _deviceLogger?.Error(error, "Соединение с SignalR закрыто");
            return RestartHubConnectionLoopAsync();
        }

        private Task RestartHubConnectionLoopAsync()
        {
            if (_hub is null) return Task.CompletedTask;
            if (Interlocked.CompareExchange(ref _hubRestartLoop, 1, 0) != 0) return Task.CompletedTask;

            return Task.Run(async () =>
            {
                try
                {
                    while (_hub.State != HubConnectionState.Connected)
                    {
                        try
                        {
                            await _hubClient.EnsureStartedAsync();
                            await JoinWorkerGroupsAsync();
                            await BroadcastWorkerAvailabilityAsync(_hardwareAvailable, _lastAvailabilityReason, force: true);
                            break;
                        }
                        catch (Exception ex)
                        {
                            _deviceLogger?.Error(ex, "Не удалось переподключиться к SignalR, повтор через 5 сек");
                            await Task.Delay(TimeSpan.FromSeconds(5));
                        }
                    }
                }
                finally
                {
                    Interlocked.Exchange(ref _hubRestartLoop, 0);
                }
            });
        }

        private async Task JoinWorkerGroupsAsync()
        {
            if (_hub is null || Controller?.Columns is null) return;

            var timeout = TimeSpan.FromSeconds(10);
            var sw = Stopwatch.StartNew();

            while (_hub.State != HubConnectionState.Connected && sw.Elapsed < timeout)
                await Task.Delay(100);

            if (_hub.State != HubConnectionState.Connected)
                throw new InvalidOperationException("Не удалось подключиться к SignalR за отведённое время");

            foreach (var column in Controller.Columns)
                await _hub.InvokeAsync("JoinController", column.GroupName, true);
        }

        private async Task BroadcastWorkerAvailabilityAsync(bool isAvailable, string? reason = null, bool force = false)
        {
            var sanitizedReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();

            if (!force &&
                _hardwareAvailable == isAvailable &&
                string.Equals(_lastAvailabilityReason ?? string.Empty, sanitizedReason ?? string.Empty, StringComparison.Ordinal))
                return;

            _hardwareAvailable = isAvailable;
            _lastAvailabilityReason = sanitizedReason;

            if (_hub is null || _hub.State != HubConnectionState.Connected) return;
            if (Controller?.Columns is null) return;

            var tasks = Controller.Columns
                .Where(c => !string.IsNullOrWhiteSpace(c.GroupName))
                .Select(c => SendAvailabilityAsync(c.GroupName, isAvailable, sanitizedReason));

            await Task.WhenAll(tasks);
        }

        private async Task SendAvailabilityAsync(string groupName, bool isAvailable, string? reason)
        {
            try
            {
                await _hub.InvokeAsync("ReportWorkerAvailability",
                    new WorkerAvailabilityReport { GroupName = groupName, IsAvailable = isAvailable, Reason = reason });
            }
            catch (Exception ex)
            {
                _deviceLogger?.Error(ex, "Не удалось отправить состояние worker для {Group}", groupName);
            }
        }

        #endregion

        #region Polling Control

        private async Task PausePollingAsync()
        {
            _pollingResumedEvent.Reset();
            // Ждём, пока текущая команда завершится
            await _commandGate.WaitAsync(_token);
            _commandGate.Release();
        }

        private Task ResumePollingAsync()
        {
            _pollingResumedEvent.Set();
            return Task.CompletedTask;
        }

        private async Task ExecuteCommandSafeAsync(Func<Task> command)
        {
            await _commandGate.WaitAsync(_token);
            try
            {
                await command();
            }
            finally
            {
                _commandGate.Release();
            }
        }

        #endregion

        #region Helpers

        private async Task CompleteFuelingAsync(Column column)
        {
            try
            {
                await Task.Delay(300, _token);
                if (column is null)
                {
                    _deviceLogger?.Warning("Колонка {GroupName} не найдена", column.GroupName);
                    return;
                }
                await ExecuteCommandAsync(Command.CompleteFueling, column.LanfengAddress);
            }
            catch (Exception e)
            {
                _deviceLogger?.Error(e, e.Message);
            }
        }

        /// <summary>
        /// Находит колонку по LanfengAddress. Для одиночного контроллера всегда возвращает первую.
        /// </summary>
        private Column? ResolveColumn(int? address)
        {
            if (_controllerType == LanfengControllerType.Single)
                return Controller.Columns.FirstOrDefault();

            return Controller.Columns.FirstOrDefault(c => c.LanfengAddress == address);
        }

        private Column? GetColumnByGroupName(string groupName) =>
            Controller.Columns.FirstOrDefault(c => c.GroupName == groupName);

        private static bool IsCriticalSerialException(Exception ex) =>
            ex is TimeoutException or IOException or InvalidOperationException or UnauthorizedAccessException;

        #endregion
    }
}
