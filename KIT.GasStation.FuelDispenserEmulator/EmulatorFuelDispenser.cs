using KIT.GasStation.Domain.Models;
using KIT.GasStation.FuelDispenser.Hubs;
using KIT.GasStation.FuelDispenser.Models;
using KIT.GasStation.FuelDispenser.Services;
using KIT.GasStation.HardwareConfigurations.Models;
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

        #endregion

        #region Public Proerties

        public Controller Controller { get; set; }

        #endregion

        #region Constructors

        public EmulatorFuelDispenser(IHubClient hubClient,
            ILogger<EmulatorFuelDispenser> logger)
        {
            _hubClient = hubClient;
            _logger = logger;
            //CreateLogger();
        }

        #endregion

        #region Override Voids

        public async Task RunAsync(CancellationToken token, Controller controller)
        {
            var opened = false;

            try
            {
                Controller = controller;
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

        public async Task StartFuelingAsync(string groupName, decimal value, bool bySum)
        {
            var cts = new CancellationTokenSource();

            var task = Task.Run(async () =>
            {
                try
                {
                    await StartFuelingLoopAsync(groupName, value, bySum, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Fueling cancelled: {GroupName}", groupName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fueling crashed: {GroupName}", groupName);
                }
                finally
                {
                    // убираем запись, только если это она (защита от гонок)
                    if (_fuelingJobs.TryGetValue(groupName, out var cur) && cur.cts == cts)
                        _fuelingJobs.TryRemove(groupName, out _);

                    cts.Dispose();
                }
            });

            _fuelingJobs[groupName] = (cts, task);
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

        private async Task StartFuelingLoopAsync(string groupName, decimal value, bool bySum, CancellationToken token)
        {
            try
            {
                await PausePollingAsync(); // если нужно — тут ок

                var column = GetColumnByGroupName(groupName);
                if (column is null)
                {
                    _logger.LogWarning("Колонка {GroupName} не найдена", groupName);
                    return;
                }

                decimal sum = bySum ? value : Math.Round(value * column.Price, 2);
                decimal quantity = bySum ? Math.Round(value / column.Price, 3) : value;

                var rnd = new Random();
                decimal receivedQuantity = 0;
                _response.GroupName = groupName;
                _response.Status = NozzleStatus.WaitingRemoved;
                await _hub.InvokeAsync("PublishStatus", _response, cancellationToken: token);

                await Task.Delay(1000, token);

                _response.Status = NozzleStatus.PumpWorking;
                await _hub.InvokeAsync("PublishStatus", _response, cancellationToken: token);

                while (true)
                {
                    token.ThrowIfCancellationRequested();

                    decimal step = (decimal)rnd.NextDouble() * 0.5m;
                    receivedQuantity += step;
                    decimal receivedSum = Math.Round(receivedQuantity * column.Price, 2);
                    if (receivedQuantity > quantity)
                    {
                        receivedQuantity = quantity;
                        receivedSum = sum;
                    }

                    var fuelingResponse = new FuelingResponse
                    {
                        GroupName = groupName,
                        Quantity = receivedQuantity,
                        Sum = receivedSum
                    };

                    await _hub.InvokeAsync("OnFuelingAsync", fuelingResponse, cancellationToken: token);

                    if (receivedQuantity >= quantity)
                        break;

                    await Task.Delay(300, token);
                }

                await Task.Delay(1000, token);

                await _hub.InvokeAsync("OnCompletedFuelingAsync", groupName, cancellationToken: _token);
            }
            finally
            {
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

        private async Task StopFuelingAsync(string groupName)
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

                await _hub.InvokeAsync("OnWaitingAsync", groupName, cancellationToken: _token);
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

        private async Task ResumeFuelingAsync(string groupName, decimal sum)
        {
            var cts = new CancellationTokenSource();

            var task = Task.Run(async () =>
            {
                try
                {
                    await StartFuelingLoopAsync(groupName, sum, true, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Fueling cancelled: {GroupName}", groupName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fueling crashed: {GroupName}", groupName);
                }
                finally
                {
                    // убираем запись, только если это она (защита от гонок)
                    if (_fuelingJobs.TryGetValue(groupName, out var cur) && cur.cts == cts)
                        _fuelingJobs.TryRemove(groupName, out _);

                    cts.Dispose();

                    await _hub.InvokeAsync("OnCompletedFuelingAsync", groupName, cancellationToken: _token);
                }
            });

            _fuelingJobs[groupName] = (cts, task);
        }

        private async Task SetPricesAsync(Dictionary<string, decimal> prices)
        {
            try
            {
                await PausePollingAsync();

                foreach (var (group, price) in prices)
                {
                    var column = GetColumnByGroupName(group);

                    if (column is null)
                    {
                        _logger.LogWarning("Колонка {GroupName} не найдена", group);
                        return;
                    }

                    column.Price = price;
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

        private async Task SetPriceAsync(string group, decimal price)
        {
            try
            {
                await PausePollingAsync();

                var column = GetColumnByGroupName(group);

                if (column is null)
                {
                    _logger.LogWarning("Колонка {GroupName} не найдена", group);
                    return;
                }

                column.Price = price;
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

        private async Task CompleteFuelingAsync(string groupName)
        {
            await _hub.InvokeAsync("OnCompletedFuelingAsync", groupName, cancellationToken: _token);
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

        public ValueTask DisposeAsync()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
