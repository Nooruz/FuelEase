using KIT.GasStation.FuelDispenser.Services;
using KIT.GasStation.FuelDispenser.Services.Factories;
using KIT.GasStation.HardwareConfigurations.Models;
using KIT.GasStation.HardwareConfigurations.Services;
using Serilog;
using System.ComponentModel;
using System.Reflection;

namespace KIT.GasStation.PKElectronics
{
    public class PKElectronicsFuelDispenser : IFuelDispenserService
    {
        #region Private Members

        private readonly IPortManager _portManager;
        private readonly IHardwareConfigurationService _hardwareConfigurationService;
        private readonly IProtocolParser _parser;
        private ISharedSerialPortService _portService;
        private int _controllerAddress;
        private ILogger _logger;
        private LanfengControllerType _lanfengControllerType;
        private bool _isStatusLoopPaused = false;
        private readonly SemaphoreSlim _statusLoopSemaphore = new(1, 1);
        private CancellationTokenSource? _statusLoopCts;
        private Task? _statusLoopTask;

        #endregion

        #region Public Properties

        public string DispenserName => Assembly.GetExecutingAssembly().GetName().Name ?? "Unknown";

        public string Version => Assembly.GetExecutingAssembly()
                                         .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                                         .InformationalVersion
                                     ?? Assembly.GetExecutingAssembly()
                                                .GetName()
                                                .Version?
                                                .ToString()
                                     ?? "Версия не найдена";

        #endregion

        #region Public Events

        public event PropertyChangedEventHandler? PropertyChanged;

        #endregion

        #region Constructors

        public PKElectronicsFuelDispenser(IPortManager portManager,
            IHardwareConfigurationService hardwareConfigurationService,
            IProtocolParserFactory protocolParserFactory)
        {
            _portManager = portManager;
            _hardwareConfigurationService = hardwareConfigurationService;
            _parser = protocolParserFactory.CreateIProtocolParser(ControllerType.PKElectronics);
        }

        #endregion

        #region Public Voids

        public async Task StartStatusPolling(int intervalMs = 200)
        {
            //if (_statusLoopCts != null)
            //    throw new InvalidOperationException("Пуллинг уже запущен");

            //if (_nozzles is { Count: > 0 })
            //{
            //    foreach (var item in _nozzles)
            //    {
            //        Column? col = await _hardwareConfigurationService.GetColumnByIdAsync(item.ColumnId);

            //        if (col != null)
            //        {
            //            item.Number = col.Nozzle;
            //        }
            //    }
            //}
            //else
            //{
            //    return;
            //}

            //Nozzle nozzle = _nozzles[0];

            //Column? column = await _hardwareConfigurationService.GetColumnByIdAsync(nozzle.ColumnId);

            //if (column == null)
            //{
            //    _logger.Error($"Колонка с идентификатором {nozzle.ColumnId} не найдена.");
            //    return;
            //}

            //// Устанавливаем адрес
            //_controllerAddress = column.Address;

            //if (column.Controller == null)
            //{
            //    _logger.Error($"Колонка с идентификатором {nozzle.ColumnId} не содержит информации о контроллере.");
            //    return;
            //}

            //_portService = await _portManager.GetPortServiceAsync(column.Controller.ComPort, column.Controller.BaudRate);

            //await InitializeAsync(nozzle.Side);

            //StatusLoopAsync(intervalMs);
        }

        public async Task Connect(string comPort, int baudRate)
        {
            try
            {
                _portService = await _portManager.GetPortServiceAsync(comPort, baudRate);
            }
            catch (Exception)
            {

            }
        }

//        public async Task<NozzleStatus> CheckStatusAsync(Column column)
//        {
//            var request = _parser.BuildRequest(Command.Status, column.Address, column.Nozzle);

//#if DEBUG
//            // Логируем запрос в отладке
//            string requestHex = BitConverter.ToString(request, 0, request.Length);
//#endif
//            var raw = await _portService.WriteReadAsync(request, expectedResponseLength: 5)
//                                                      .ConfigureAwait(false);

//#if DEBUG
//            // Логируем ответ в отладке
//            string responseHex = BitConverter.ToString(raw, 0, raw.Length);
//#endif

//            var response = _parser.ParseResponse(raw, Command.Status);

//            return response.Status;
//        }

//        public async Task SetPriceAsync(Nozzle nozzle, decimal? price = null)
//        {
//            try
//            {
//                await PauseStatusLoopAsync();

//                var request = _parser
//                    .BuildRequest(Command.ChangePrice, _controllerAddress, nozzle.Number, nozzle.Price);

//                _logger.Information($"[Tx] {BitConverter.ToString(request)}");

//#if DEBUG
//                // Отладочный вывод запроса
//                string debugRequest = BitConverter.ToString(request, 0, request.Length);
//#endif

//                var raw = await _portService.WriteReadAsync(request, expectedResponseLength: 4)
//                                                          .ConfigureAwait(false);

//                _logger.Information($"[Rx] {BitConverter.ToString(raw)}");

//#if DEBUG
//                // Логируем ответ в отладке
//                string rawHex = BitConverter.ToString(raw, 0, raw.Length);
//#endif
//            }
//            catch (Exception ex)
//            {
//                // Логируем ошибку и пробрасываем выше
//                _logger.Error(ex, "Ошибка при изменении цены");
//                throw;
//            }
//            finally
//            {
//                ResumeStatusLoop();
//            }
//        }

//        public async Task StartRefuelingSumAsync(Nozzle nozzle, decimal? sum = null)
//        {
//            try
//            {
//                // Приостанавливаем цикл опроса статуса
//                await PauseStatusLoopAsync();

//                var request = _parser
//                    .BuildRequest(Command.StartFillingSum, _controllerAddress, nozzle.Number, nozzle.FuelSale.Sum, nozzle.FuelSale.Quantity);

//                _logger.Information($"[Tx] {BitConverter.ToString(request)}");

//#if DEBUG
//                // Отладочный вывод запроса
//                string debugRequest = BitConverter.ToString(request, 0, request.Length);
//#endif

//                var raw = await _portService.WriteReadAsync(request, expectedResponseLength: 4)
//                                                          .ConfigureAwait(false);

//                _logger.Information($"[Rx] {BitConverter.ToString(raw)}");

//#if DEBUG
//                // Логируем ответ в отладке
//                string rawHex = BitConverter.ToString(raw, 0, raw.Length);
//#endif

//            }
//            catch (Exception ex)
//            {
//                _logger.Error(ex, "Ошибка при начале заправки на колонке [{ColumnId}].", nozzle.Tube);
//            }
//            finally
//            {
//                // Возобновляем цикл опроса статуса
//                ResumeStatusLoop();
//            }
//        }

        #endregion

        #region Private Voids

        private void StatusLoopAsync(int intervalMs)
        {
//            // Если уже запущен, повторно не запускаем
//            if (_statusLoopCts != null)
//            {
//#if DEBUG
//                _logger.Warning("Цикл статуса уже запущен. Повторный вызов StartStatusLoopAsync игнорируется.");
//#endif
//                return;
//            }

//            // Создаём токен-источник, чтобы можно было прервать цикл
//            _statusLoopCts = new CancellationTokenSource();

//            _statusLoopTask = Task.Run(async () =>
//            {
//#if DEBUG
//                _logger.Information("Начат фоновый цикл опроса статуса с интервалом {Interval} мс.", intervalMs);
//#endif

//                while (!_statusLoopCts.IsCancellationRequested)
//                {
//                    bool semaphoreAcquired = false;
//                    bool isStartedRefueling = false;
//                    Nozzle readingNozzle = new();
//                    foreach (var nozzle in _nozzles)
//                    {
//                        try
//                        {
//                            // Ожидаем освобождения семафора
//                            await _statusLoopSemaphore.WaitAsync(_statusLoopCts.Token);
//                            semaphoreAcquired = true;

//                            var request = _parser.BuildRequest(Command.Status, _controllerAddress, nozzle.Number);

//                            _logger.Information($"[Tx] {BitConverter.ToString(request)}");

//#if DEBUG
//                            // Отладочный вывод запроса
//                            string debugRequest = BitConverter.ToString(request);
//#endif
//                            var raw = await _portService.WriteReadAsync(request, expectedResponseLength: 5,
//                                writeTimeout: 20).ConfigureAwait(false);

//                            _logger.Information($"[Rx] {BitConverter.ToString(raw)}");

//                            var response = _parser.ParseResponse(raw, Command.Status);

//                            if (response.IsValid)
//                            {
//                                if (response.Status is NozzleStatus.PumpWorking or NozzleStatus.WaitingStop)
//                                {
//                                    isStartedRefueling = true;
//                                    readingNozzle = nozzle;
//                                }
//                                nozzle.Status = response.Status;
//                                nozzle.ReceivedQuantity = response.Quantity;
//                                nozzle.ReceivedSum = response.Sum;
//                                nozzle.Lifted = response.IsLifted;
//                            }
//                        }
//                        catch (TaskCanceledException)
//                        {
//                            break; // Выходим из цикла при отмене
//                        }
//                        catch (Exception ex)
//                        {
//                            _logger.Error(ex, ex.Message);
//                        }
//                        finally
//                        {
//                            if (semaphoreAcquired)
//                            {
//                                _statusLoopSemaphore.Release();
//                            }
//                        }

//                        await Task.Delay(intervalMs, _statusLoopCts.Token).ConfigureAwait(false);
//                    }

//                    if (isStartedRefueling)
//                    {
//                        await ReadingDataFromScreen(readingNozzle);
//                    }
//                }

//            }, _statusLoopCts.Token);
        }

        private async Task InitializeAsync(int side)
        {
            //try
            //{
            //    InitLog(side);

            //    await InitStatusPollingAsync();

            //    foreach (var nozzle in _nozzles)
            //    {
            //        await SensorPollingAsync();


            //    }

            //}
            //catch (Exception ex)
            //{
            //    _logger.Error(ex, "Ошибка при инициализации колонки.");
            //}
        }

        private async Task InitStatusPollingAsync()
        {
//            foreach (var nozzle in _nozzles)
//            {
//                try
//                {
//                    var request = _parser.BuildRequest(Command.Status, _controllerAddress, nozzle.Number);

//                    _logger.Information($"[Tx] {BitConverter.ToString(request)}");

//#if DEBUG
//                    // Отладочный вывод запроса
//                    string debugRequest = BitConverter.ToString(request);
//#endif
//                    var raw = await _portService.WriteReadAsync(request, expectedResponseLength: 5,
//                        writeTimeout: 20).ConfigureAwait(false);

//                    _logger.Information($"[Rx] {BitConverter.ToString(raw)}");

//                    var response = _parser.ParseResponse(raw, Command.Status);

//                    if (response.IsValid)
//                    {
//                        nozzle.Status = response.Status;
//                        nozzle.ReceivedQuantity = response.Quantity;
//                        nozzle.ReceivedSum = response.Sum;
//                        nozzle.Lifted = response.IsLifted;
//                    }
//                }
//                catch (OperationCanceledException)
//                {
//                    // Ожидаемо при остановке
//                }
//                catch (Exception ex)
//                {
//                    //OnConnectionLost?.Invoke(ex);
//                }
//            }
        }

        private async Task SensorPollingAsync()
        {
//            try
//            {
//                Nozzle nozzle = _nozzles.First();

//                var request = _parser.BuildRequest(Command.Sensor, _controllerAddress, nozzle.Number);

//                _logger.Information($"[Tx] {BitConverter.ToString(request)}");

//#if DEBUG
//                // Отладочный вывод запроса
//                string debugRequest = BitConverter.ToString(request);
//#endif
//                var raw = await _portService.WriteReadAsync(request, expectedResponseLength: 5,
//                    writeTimeout: 20).ConfigureAwait(false);

//                _logger.Information($"[Rx] {BitConverter.ToString(raw)}");
//            }
//            catch (OperationCanceledException)
//            {
//                // Ожидаемо при остановке
//            }
//            catch (Exception ex)
//            {
//                //OnConnectionLost?.Invoke(ex);
//            }
        }

//        private async Task ReadingDataFromScreen(Nozzle nozzle)
//        {
//            var request = _parser.BuildRequest(Command.Screen, _controllerAddress, nozzle.Number);

//            _logger.Information($"[Tx] {BitConverter.ToString(request)}");

//#if DEBUG
//            // Отладочный вывод запроса
//            string debugRequest = BitConverter.ToString(request);
//#endif
//            var raw = await _portService.WriteReadAsync(request, expectedResponseLength: 5,
//                writeTimeout: 20).ConfigureAwait(false);

//            _logger.Information($"[Rx] {BitConverter.ToString(raw)}");

//            var response = _parser.ParseResponse(raw, Command.Screen);

//            if (response.IsValid)
//            {
//                // Извлекаем 4-й и 5-й байты
//                byte byte4 = raw[3]; // Индексация массива начинается с 0
//                byte byte5 = raw[4];
                
//                // Объединяем 4-й и 5-й байты в одно 16-битное число
//                int combinedValue = (byte4 << 8) | byte5; // Преобразуем к int, объединяя два байта

//                // Пример: combinedValue = 0x0BD6 = 3030 в десятичной системе (3 литра и 30 грамм)
//                decimal liters = (decimal)(combinedValue / 1000.0); // Преобразуем в литры и граммы (3030 -> 3.030 литра)

//                nozzle.ReceivedQuantity = response.Quantity;
//                nozzle.ReceivedSum = response.Sum;
//            }
//        }

        #endregion

        #region StartStopStatusLoopAsync

        private async Task PauseStatusLoopAsync()
        {
            if (_isStatusLoopPaused)
            {
#if DEBUG
                _logger.Warning("Опрос статуса уже приостановлен.");
#endif
                return;
            }

            await _statusLoopSemaphore.WaitAsync();
            _isStatusLoopPaused = true;
#if DEBUG
            _logger.Information("Опрос статуса приостановлен.");
#endif
        }

        private void ResumeStatusLoop()
        {
            if (!_isStatusLoopPaused)
            {
#if DEBUG
                _logger.Warning("Опрос статуса не приостановлен.");
#endif
                return;
            }

            _statusLoopSemaphore.Release();
            _isStatusLoopPaused = false;
#if DEBUG
            _logger.Information("Опрос статуса возобновлён.");
#endif
        }

        /// <summary>
        /// Останавливает фоновый цикл опроса статуса.
        /// </summary>
        private void StopStatusLoop()
        {
            if (_statusLoopCts == null)
                return;

            _statusLoopCts.Cancel();

            try
            {
                _statusLoopTask?.Wait();
            }
            catch
            {
                // Игнорируем ошибки при завершении
            }

            _statusLoopCts.Dispose();
            _statusLoopCts = null;
            _statusLoopTask = null;
        }

        #endregion

        #region Logs

        /// <summary>
        /// Инициализация логгера.
        /// </summary>
        private void InitLog(int side)
        {
            // 1. Создадим/убедимся, что существует папка logs
            var logsDir = Path.Combine(AppContext.BaseDirectory, "logs");
            Directory.CreateDirectory(logsDir);

            // 2. Формируем имя файла. Можно добавить время, 
            //    но обязательно без «:» (двоеточий). Например, yyyy-MM-dd_HH-mm-ss.
            var logFilePath = Path.Combine(logsDir, $"{nameof(PKElectronicsFuelDispenser)}_{side}_{DateTime.Now:yyyyddMM}.log");

            // 3. Настраиваем Serilog
            _logger = new LoggerConfiguration()
                // Указываем минимальный уровень
                .MinimumLevel.Debug()
                // Пишем в файл с «дневным» ротационным интервалом
                .WriteTo.File(
                    path: logFilePath,
                    rollingInterval: RollingInterval.Day,
                    // Можно задать, сколько файлов хранить
                    retainedFileCountLimit: 7,
                    // Можно включить автопереход на новый файл при достижении лимита размера
                    rollOnFileSizeLimit: true
                )
                // При желании можно добавить вывод в консоль
                //.WriteTo.Console()
                .CreateLogger();

            // 4. Пробный лог на уровне Information
            _logger.Information($"---------------ПК-Электроникс инициализирован. [{DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.fff")}]---------------");
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            // Останавливаем цикл статусов и освобождаем ресурсы
            StopStatusLoop();
            _statusLoopSemaphore.Dispose();
            _portService?.Dispose();
        }

        #endregion
    }
}
