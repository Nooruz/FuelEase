using KIT.GasStation.Domain.Models;
using KIT.GasStation.FuelDispenser;
using KIT.GasStation.FuelDispenser.Hubs;
using KIT.GasStation.FuelDispenser.Models;
using KIT.GasStation.HardwareConfigurations.Models;
using KIT.GasStation.HardwareConfigurations.Services;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace KIT.GasStation.Emulator
{
    public sealed class EmulatorFuelDispenser : IFuelDispenserService
    {
        #region Private Members

        private readonly IHubClient _hubClient;
        private HubConnection _hub;
        private readonly ILogger<EmulatorFuelDispenser> _logger;
        private Task _pollingTask;
        private readonly object _pollLock = new();
        private volatile bool _pollingEnabled;
        private readonly StatusResponse _response = new() { Status = NozzleStatus.Ready };
        private CancellationToken _token;
        private readonly ConcurrentDictionary<string, (CancellationTokenSource cts, Task task)> _fuelingJobs = new();
        private readonly IHardwareConfigurationService _hardwareConfigurationService;

        #endregion

        #region Public Proerties

        public Controller Controller { get; set; }

        #endregion

        #region Constructors

        public EmulatorFuelDispenser(IHubClient hubClient,
            ILogger<EmulatorFuelDispenser> logger,
            IHardwareConfigurationService hardwareConfigurationService)
        {
            _hubClient = hubClient;
            _logger = logger;
            _hardwareConfigurationService = hardwareConfigurationService;
            //CreateLogger();
        }

        #endregion

        #region Override Voids

        public async Task RunAsync(CancellationToken token)
        {
            var opened = false;

            try
            {
                await OnOpenAsync(token);
                opened = true;

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        // Краткая задержка между тиками, если OnTickAsync завершается быстро
                        await Task.Delay(TimeSpan.FromMilliseconds(100), token);
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested)
                    {
                        // Ожидаем штатное завершение по токену отмены
                        break;
                    }
                    catch (Exception)
                    {
                        // Логируем ошибку, но продолжаем цикл
                        // Реализуйте ILogger в базовом классе или передайте его через конструктор
                        //_logger.Error(ex, "Ошибка в OnTickAsync");
                        await Task.Delay(TimeSpan.FromSeconds(1), token);
                    }
                }

                await Task.Delay(Timeout.Infinite, token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                _logger.LogInformation("Цикл ТРК отменен по токену");
            }
            finally
            {
                if (opened)
                {
                    await OnCloseAsync();
                    _logger.LogInformation("Цикл ТРК завершен");
                }
            }
        }

        public async Task StartFuelingAsync(FuelingRequest fuelingRequest)
        {
            var cts = new CancellationTokenSource();

            var task = Task.Run(async () =>
            {
                try
                {
                    await StartFuelingLoopAsync(fuelingRequest, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Fueling cancelled: {GroupName}", fuelingRequest.GroupName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fueling crashed: {GroupName}", fuelingRequest.GroupName);
                }
                finally
                {
                    // убираем запись, только если это она (защита от гонок)
                    if (_fuelingJobs.TryGetValue(fuelingRequest.GroupName, out var cur) && cur.cts == cts)
                        _fuelingJobs.TryRemove(fuelingRequest.GroupName, out _);

                    cts.Dispose();
                }
            });

            _fuelingJobs[fuelingRequest.GroupName] = (cts, task);
        }

        public async Task GetCounterAsync(Guid commandId, string groupName)
        {
            try
            {
                await PausePollingAsync();
                var column = GetColumnByGroupName(groupName);
                if (column is null) return;

                await _hub.InvokeAsync("CounterUpdated",
                new CounterData { GroupName = column.GroupName, Counter = column.SystemCounter },
                cancellationToken: _token);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
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
                    await _hub.InvokeAsync("CounterUpdated",
                    new CounterData { GroupName = column.GroupName, Counter = column.SystemCounter },
                    cancellationToken: _token);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
            finally
            {
                await ResumePollingAsync();
            }
        }

        public Task ChangeControlModeAsync(Guid commandId, string groupName, bool isProgramMode) => Task.CompletedTask;

        public Task InitializeConfigurationAsync(Guid commandId, string groupName) => Task.CompletedTask;

        public async Task SetPriceAsync(Guid commandId, PriceRequest priceRequest)
        {
            try
            {
                await PausePollingAsync();

                var column = GetColumnByGroupName(priceRequest.GroupName);

                if (column is null)
                {
                    _logger.LogWarning("Колонка {GroupName} не найдена", priceRequest.GroupName);
                    return;
                }

                column.Price = priceRequest.Value;

                if (column.Settings is EmulatorColumnSettings settings)
                {
                    settings.LastPrice = priceRequest.Value;
                    await _hardwareConfigurationService.SaveColumnAsync(column);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
            finally
            {
                await ResumePollingAsync();
            }
        }

        public ValueTask DisposeAsync()
        {
            throw new NotImplementedException();
        }

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
                        _logger.LogWarning("Колонка {GroupName} не найдена", request.GroupName);
                        return;
                    }

                    column.Price = request.Value;

                    if (column.Settings is EmulatorColumnSettings settings)
                    {
                        settings.LastPrice = request.Value;
                        await _hardwareConfigurationService.SaveColumnAsync(column);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
            finally
            {
                await ResumePollingAsync();
            }
        }

        public Task GetStatusByAddressAsync(string groupName) => Task.CompletedTask;

        public async Task StopFuelingAsync(string groupName)
        {
            try
            {
                await PausePollingAsync();

                StopFueling(groupName);

                var column = GetColumnByGroupName(groupName);
                if (column is null)
                {
                    _logger.LogWarning("Колонка {GroupName} не найдена", groupName);
                    return;
                }
                _response.GroupName = groupName;
                _response.Status = NozzleStatus.WaitingRemoved;
                await _hub.InvokeAsync("PublishStatus", _response, cancellationToken: _token);

                await Task.Delay(500, _token);

                await _hub.InvokeAsync("WaitingAsync", groupName, cancellationToken: _token);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
            finally
            {
                await ResumePollingAsync();
            }
        }

        public async Task ResumeFuelingAsync(ResumeFuelingRequest resumeFuelingRequest)
        {
            var cts = new CancellationTokenSource();

            var task = Task.Run(async () =>
            {
                try
                {
                    var request = new FuelingRequest
                    {
                        GroupName = resumeFuelingRequest.GroupName,
                        Sum = resumeFuelingRequest.Sum,
                        Quantity = resumeFuelingRequest.Quantity,
                        FuelingStartMode = FuelingStartMode.ByAmount // для примера, можно расширить модель запроса
                    };

                    await StartFuelingLoopAsync(request, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Fueling cancelled: {GroupName}", resumeFuelingRequest.GroupName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fueling crashed: {GroupName}", resumeFuelingRequest.GroupName);
                }
                finally
                {
                    // убираем запись, только если это она (защита от гонок)
                    if (_fuelingJobs.TryGetValue(resumeFuelingRequest.GroupName, out var cur) && cur.cts == cts)
                        _fuelingJobs.TryRemove(resumeFuelingRequest.GroupName, out _);

                    cts.Dispose();
                }
            });

            _fuelingJobs[resumeFuelingRequest.GroupName] = (cts, task);
        }

        public async Task CompleteFuelingAsync(string groupName)
        {
            var column = GetColumnByGroupName(groupName);
            if (column is null)
            {
                _logger.LogWarning("Колонка {GroupName} не найдена", groupName);
                return;
            }

            if (column.Settings is EmulatorColumnSettings settings)
            {
                await _hub.InvokeAsync("CompletedFuelingAsync", groupName, settings.ReceivedQuantity, cancellationToken: _token);
            }
        }

        #endregion

        #region Private Voids

        private async Task OnTickAsync()
        {
            _logger.LogInformation("ТРК Эмулятор запущена");

            var column = Controller.Columns.First();

            while (!_token.IsCancellationRequested && _pollingEnabled)
            {
                try
                {
                    // Ждем, если опрос приостановлен
                    _pollingResumedEvent.Wait(_token);

                    _response.GroupName = column.GroupName;
                    _response.Status = NozzleStatus.Ready;

                    await _hub.InvokeAsync("PublishStatus", _response, cancellationToken: _token);
                }
                catch (OperationCanceledException ex) when (ex.CancellationToken == _token)
                {
                    _logger.LogInformation("Опрос ТРК Эмулятор отменён: {Message}", ex.Message);
                    break;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, e.Message, e.StackTrace);
                    await Task.Delay(1000, _token);
                }
                if (!_pollingEnabled) break;
            }
        }

        private async Task OnOpenAsync(CancellationToken token)
        {
            try
            {
                _token = token;
                _hub = _hubClient.Connection;

                //_logger.LogInformation("Начало инициализации ТРК {Id}. Состояние HubConnection: {State}", Controller.Id, _hub?.State.ToString() ?? "null");

                _logger.LogDebug("EnsureStartedAsync вызван. Текущее состояние: {State}", _hub.State);

                // 1. СНАЧАЛА запускаем SignalR (синхронно)
                //await _hubClient.EnsureStartedAsync(token);

                // 3. Присоединяемся к группам

                await StartPollingAsync();

            }
            catch (Exception e)
            {
                _logger.LogError(e, "Ошибка в OnOpenAsync: {Message}", e.Message);
                throw;
            }
        }

        private async Task OnCloseAsync()
        {
            await StopPollingAsync();
        }

        private async Task StartPollingAsync()
        {
            if (_pollingTask == null || _pollingTask.IsCompleted)
            {
                lock (_pollLock)
                {
                    if (_pollingEnabled) return;      // уже запущено
                }

                try
                {
                    // 2) Только после успешного Acquire включаем флаг
                    lock (_pollLock)
                    {
                        _pollingEnabled = true;
                    }

                    // 3) Стартуем цикл опроса (lease остаётся жить в поле)
                    _pollingTask = OnTickAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Не удалось стартовать polling");
                    lock (_pollLock) { _pollingEnabled = false; }
                }
            }

            var column = Controller.Columns.First();

            _response.GroupName = column.GroupName;

            await _hub.InvokeAsync("PublishStatus", _response, cancellationToken: _token);
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

            if (toAwait != null)
                await toAwait;
        }

        private async Task StartFuelingLoopAsync(FuelingRequest fuelingRequest, CancellationToken token)
        {
            if (fuelingRequest is null)
                throw new ArgumentNullException(nameof(fuelingRequest));

            var pollingPaused = false;

            try
            {
                await PausePollingAsync();
                pollingPaused = true;

                var column = GetColumnByGroupName(fuelingRequest.GroupName);
                if (column is null)
                {
                    _logger.LogWarning("Колонка {GroupName} не найдена", fuelingRequest.GroupName);
                    return;
                }

                if (column.Price <= 0)
                {
                    _logger.LogWarning(
                        "Для колонки {GroupName} указана некорректная цена: {Price}",
                        fuelingRequest.GroupName,
                        column.Price);
                    return;
                }

                if (fuelingRequest.Quantity <= 0)
                {
                    _logger.LogWarning(
                        "Для колонки {GroupName} передано некорректное количество: {Quantity}",
                        fuelingRequest.GroupName,
                        fuelingRequest.Quantity);
                    return;
                }

                if (column.Settings is EmulatorColumnSettings setting)
                {
                    setting.LastQuantity = fuelingRequest.Quantity;
                    setting.LastSum = fuelingRequest.Sum;
                    await _hardwareConfigurationService.SaveColumnAsync(column);
                }

                decimal quantity = fuelingRequest.Quantity;
                decimal sum = fuelingRequest.Sum;

                var random = Random.Shared;
                decimal receivedQuantity = 0m;
                decimal lastSavedQuantity = 0m;

                var statusResponse = new StatusResponse
                {
                    GroupName = fuelingRequest.GroupName,
                    Status = NozzleStatus.WaitingRemoved
                };

                await _hub.InvokeAsync("PublishStatus", statusResponse, cancellationToken: token);

                await Task.Delay(1000, token);

                statusResponse.Status = NozzleStatus.PumpWorking;
                await _hub.InvokeAsync("PublishStatus", statusResponse, cancellationToken: token);

                while (true)
                {
                    token.ThrowIfCancellationRequested();

                    decimal step = decimal.Round(random.Next(0, 500) / 1000m, 2, MidpointRounding.AwayFromZero);

                    receivedQuantity = decimal.Round(receivedQuantity + step, 2, MidpointRounding.AwayFromZero);
                    decimal receivedSum = decimal.Round(receivedQuantity * column.Price, 2, MidpointRounding.AwayFromZero);

                    if (receivedQuantity > quantity)
                    {
                        receivedQuantity = quantity;
                        receivedSum = sum;
                    }

                    decimal deltaQuantity = decimal.Round(receivedQuantity - lastSavedQuantity, 2, MidpointRounding.AwayFromZero);

                    if (deltaQuantity > 0)
                    {
                        column.SystemCounter = decimal.Round(column.SystemCounter + deltaQuantity, 2, MidpointRounding.AwayFromZero);
                        lastSavedQuantity = receivedQuantity;

                        if (column.Settings is EmulatorColumnSettings settings)
                        {
                            settings.ReceivedSum = receivedSum;
                            settings.ReceivedQuantity = receivedQuantity;
                        }

                        await _hardwareConfigurationService.SaveColumnAsync(column);
                    }

                    var fuelingResponse = new FuelingResponse
                    {
                        GroupName = fuelingRequest.GroupName,
                        Quantity = receivedQuantity,
                        Sum = receivedSum
                    };

                    await _hub.InvokeAsync("FuelingAsync", fuelingResponse, cancellationToken: token);

                    if (receivedQuantity >= quantity)
                        break;

                    await Task.Delay(300, token);
                }

                await Task.Delay(1000, token);

                await _hub.InvokeAsync(
                    "CompletedFuelingAsync",
                    fuelingRequest.GroupName,
                    receivedQuantity,
                    cancellationToken: token);
            }
            finally
            {
                if (pollingPaused)
                    await ResumePollingAsync();
            }
        }

        private void StopFueling(string groupName)
        {
            if (_fuelingJobs.TryRemove(groupName, out var job))
            {
                job.cts.Cancel();
                // Dispose будет в finally у таски — так безопаснее
            }
        }

        #endregion

        #region Helpers

        private Column? GetColumnByGroupName(string groupName)
        {
            return Controller.Columns.FirstOrDefault(c => c.GroupName == groupName);
        }

        #endregion

        #region Пауза и продолжения опроса

        private readonly ManualResetEventSlim _pollingResumedEvent = new(true);
        private readonly SemaphoreSlim _commandGate = new(1, 1);

        private async Task PausePollingAsync()
        {
            _pollingResumedEvent.Reset();

            // ⛔ ждём, пока текущая команда ДОРАБОТАЕТ
            await _commandGate.WaitAsync();
            _commandGate.Release();
        }

        private async Task ResumePollingAsync()
        {
            _pollingResumedEvent.Set();
            await Task.CompletedTask;
        }

        private async Task ExecuteCommandSafeAsync(
            Func<Task> command,
            CancellationToken token)
        {
            await _commandGate.WaitAsync(token);
            try
            {
                await command(); // <-- вот тут команда гарантированно ДОРАБАТЫВАЕТ
            }
            finally
            {
                _commandGate.Release();
            }
        }

        #endregion

        #region Logs

        private void CreateLogger()
        {
            // создаём общую папку для логов ТРК
            var logRoot = Path.Combine(AppContext.BaseDirectory, "logs", "trk");
            Directory.CreateDirectory(logRoot);

            // безопасное имя файла (уникальное для экземпляра)
            string safeController = Sanitize(Controller.Name);
            string safePort = Sanitize(Controller.ComPort);
            string controllerId = Controller.Id == Guid.Empty ? "noid" : Controller.Id.ToString("N");
            string fileName = $"TRK_{Controller.Type}_{safeController}_{safePort}_{controllerId}_{Controller.Address}.log";
            string path = Path.Combine(logRoot, fileName);

            // отдельный Serilog для файла инстанса
            //_logger = new LoggerConfiguration()
            //    .MinimumLevel.LogDebug()
            //    .Enrich.WithProperty("Controller", Controller.Name)
            //    .Enrich.WithProperty("Address", Address)
            //    .Enrich.WithProperty("ComPort", Controller.ComPort)
            //    .WriteTo.File(
            //        path: path,
            //        rollingInterval: RollingInterval.Day,
            //        retainedFileCountLimit: 14,
            //        shared: true,
            //        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
            //    )
            //    .CreateLogger();

            _logger.LogInformation("Инициализация Emulator для {Controller}/{Address} на порту {Port}",
            Controller.Name, Controller.Address, Controller.ComPort);
        }

        private static string Sanitize(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "UNNAMED";
            s = s.Trim();
            s = Regex.Replace(s, @"[^\w\-\.\(\) ]+", "_"); // заменяем недопустимые символы
            return s.Length > 80 ? s[..80] : s;
        }

        #endregion
    }
}
