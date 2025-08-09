using KIT.GasStation.FuelDispenser.Commands;
using KIT.GasStation.FuelDispenser.Services;
using KIT.GasStation.HardwareConfigurations.Models;
using KIT.GasStation.HardwareConfigurations.Services;
using KIT.GasStation.Lanfeng.Utilities;
using Serilog;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;

namespace KIT.GasStation.Lanfeng
{
    /// <summary>
    /// Класс для управления колонкой Lanfeng.
    /// </summary>
    public class LanfengFuelDispenser2
    {
//        #region Private Members

//        private readonly IPortManager _portManager;
//        private readonly IHardwareConfigurationService _hardwareConfigurationService;
//        private readonly IFuelSaleService _fuelSaleService;
//        private ISharedSerialPortService _sharedSerialPortService;
//        private ILogger _logger;
//        private ObservableCollection<Column> _columns;
//        private ObservableCollection<Nozzle> _nozzles;
//        private CancellationTokenSource? _statusLoopCts;
//        private Task? _statusLoopTask;
//        private int _address;
//        private LanfengControllerType _lanfengControllerType;
//        private readonly SemaphoreSlim _statusLoopSemaphore = new(1, 1);
//        private bool _isStatusLoopPaused = false;
//        private Nozzle _selectedNozzle;

//        // Для фильтрации повторных вызовов событий
//        private readonly ConcurrentDictionary<int, decimal> _lastCounter = new();
//        private readonly ConcurrentDictionary<Guid, bool> _lastLiftState = new();
//        private DateTime _lastConnectionLost = DateTime.MinValue;

//        #endregion

//        #region Actions

//        /// <inheritdoc/>
//        //public event Action<Guid, NozzleStatus> OnStatusChanged;

//        /// <inheritdoc/>
//        //public event Action<Guid, decimal> OnCounterReceived;

//        /// <inheritdoc/>
//        public event Action<Guid> OnColumnLifted;

//        /// <inheritdoc/>
//        public event Action OnColumnLowered;

//        /// <inheritdoc/>
//        //public event Action<Guid, decimal, decimal> OnStartedFilling;

//        /// <inheritdoc/>
//        public event Action<int> OnWaitingRemoved;

//        /// <inheritdoc/>
//        public event Action<int> OnCompletedFilling;

//        /// <inheritdoc/>
//        public event Action OnConnectionLost;

//        #endregion

//        #region Public Proeprties

//        /// <inheritdoc/>
//        public string DispenserName => Assembly.GetExecutingAssembly().GetName().Name ?? "Unknown";

//        /// <inheritdoc/>
//        public string Version => Assembly.GetExecutingAssembly()
//                                         .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
//                                         .InformationalVersion
//                                     ?? Assembly.GetExecutingAssembly()
//                                                .GetName()
//                                                .Version?
//                                                .ToString()
//                                     ?? "Версия не найдена";

//        public NozzleStatus Status {  get; set; }

//        #endregion

//        #region Constructors

//        /// <summary>
//        /// Конструктор класса LanfengFuelDispenser.
//        /// </summary>
//        /// <param name="portManager">Менеджер портов.</param>
//        /// <param name="hardwareConfigurationService"></param>
//        public LanfengFuelDispenser2(IPortManager portManager, 
//            IHardwareConfigurationService hardwareConfigurationService,
//            IFuelSaleService fuelSaleService)
//        {
//            _portManager = portManager;
//            _fuelSaleService = fuelSaleService;
//            _hardwareConfigurationService = hardwareConfigurationService;
//        }

//        #endregion

//        #region Public Voids

//        /// <inheritdoc/>
//        public async Task Connect(string comPort, int baudRate)
//        {
//            try
//            {
//                _sharedSerialPortService = await _portManager.GetPortServiceAsync(comPort, baudRate);
//            }
//            catch (Exception)
//            {

//            }
//        }

//        /// <inheritdoc/>
//        public async Task Connect(ObservableCollection<Nozzle> nozzles)
//        {
//            try
//            {
//                _nozzles = nozzles;

//                if (_nozzles is { Count: > 0 })
//                {
//                    foreach (var item in _nozzles)
//                    {
//                        Column? col = await _hardwareConfigurationService.GetColumnByIdAsync(item.ColumnId);

//                        if (col != null)
//                        {
//                            item.Number = col.Nozzle;
//                        }
//                    }
//                }
//                else
//                {
//                    return;
//                }

//                Nozzle nozzle = _nozzles[0];

//                Column? column = await _hardwareConfigurationService.GetColumnByIdAsync(nozzle.ColumnId);

//                if (column == null)
//                {
//                    _logger.Error($"Колонка с идентификатором {nozzle.ColumnId} не найдена.");
//                    return;
//                }

//                // Устанавливаем адрес
//                _address = column.Address;

//                if (column.Controller == null)
//                {
//                    _logger.Error($"Колонка с идентификатором {nozzle.ColumnId} не содержит информации о контроллере.");
//                    return;
//                }

//                _columns = await _hardwareConfigurationService.GetColumnsByControllerIdAndAddressAsync(column.Controller.Id, column.Address);

//                _sharedSerialPortService = await _portManager.GetPortServiceAsync(column.Controller.ComPort, column.Controller.BaudRate);
//            }
//            catch (Exception)
//            {

//            }
//        }

//        /// <inheritdoc/>
//        public async Task GetCountersAsync(Nozzle nozzle)
//        {
//            try
//            {
//                // Приостанавливаем цикл опроса статуса
//                await PauseStatusLoopAsync();

//                var request = new byte[14];
//                request[0] = 0xA5;
//                request[1] = (byte)(GetNozzleAddress(nozzle) << 4 | _address);
//                request[2] = (byte)Command.CounterLiter;
//                request[13] = ChecksumHelper.CalculateChecksum(request);
//                await SendAndParseAsync(request);
//            }
//            catch (Exception ex)
//            {
//                _logger.Error(ex, "Ошибка при запросе счетчика литров на колонке [{ColumnId}].", nozzle.Tube);
//            }
//            finally
//            {
//                // Возобновляем цикл опроса статуса
//                ResumeStatusLoop();
//            }
//        }

//        /// <inheritdoc/>
//        public async Task<NozzleStatus> GetStatusAsync(int tube)
//        {
//            Nozzle? nozzle = GetNozzleByTube(tube);
//            if (nozzle == null)
//            {
//                _logger.Error($"Колонка {tube} не найдена.");
//                return NozzleStatus.Unknown;
//            }

//            var request = GetStatusBytes();

//            await SendAndParseAsync(request);

//            return Status;
//        }

//        /// <inheritdoc/>
//        public async Task<NozzleStatus> CheckStatusAsync(Column column)
//        {
//            // Устанавливаем адрес
//            _address = column.Address;

//            _logger.Information($"Начинаем проверку статуса для колонки [{column.Id}] (адрес: {column.Address}).");

//            // Формируем запрос на получение статуса колонки
//            var request = GetStatusBytes();
//            _logger.Debug("Сформирован запрос на статус колонки: {RequestBytes}", BitConverter.ToString(request));

//            try
//            {
//                // Отправляем запрос на колонку
//                _logger.Debug("Отправляем запрос на колонку [{ColumnId}]...", column.Id);
//                var response = await _sharedSerialPortService.WriteReadAsync(request, 14, writeTimeout: 300);

//                if (response == null || response.Length < 12)
//                {
//                    _logger.Warning(
//                        "Ответ от колонки [{ColumnId}] получен, но он пустой или недостаточной длины (Length={Length}).",
//                        column.Id,
//                        response?.Length ?? 0
//                    );
//                    return NozzleStatus.Unknown;
//                }

//                // Логируем «сырые» данные ответа
//                _logger.Debug("Получен ответ от колонки [{ColumnId}]: {ResponseBytes}",
//                    column.Id,
//                    BitConverter.ToString(response));

//                // Парсим ответ и получаем адрес и статус колонки
//                var (address, status) = ParseStatus(response[11]);
//                _logger.Debug("Из ответа: адрес = {Address}, статус = {Status}", address, status);

//                // Возвращаем статус колонки
//                _logger.Information(
//                    "Завершена проверка статуса колонки [{ColumnId}]. Текущий статус: {NozzleStatus}",
//                    column.Id,
//                    status
//                );

//                // Возвращаем статус колонки
//                return status;
//            }
//            catch (Exception ex)
//            {
//                // Логируем ошибку
//                _logger.Error(ex, "Ошибка при проверке статуса колонки [{ColumnId}].", column.Id);
//                return NozzleStatus.Unknown;
//            }
//        }

//        /// <inheritdoc/>
//        public async Task InitializeAsync(int side)
//        {
//            try
//            {
//                InitLog(side);

//                await GetStatusAsync();

//                if (Status != NozzleStatus.Unknown)
//                {
//                    // Программное управление
//                    await ProgramControlAsync();

//                    // Получаем версию прошивки
//                    await GetFirmwareVersionAsync();

//                    // Инициализация по пистолетам
//                    await InitializeByNozzles();
//                }
//            }
//            catch (Exception ex)
//            {
//                _logger.Error(ex, "Ошибка при инициализации колонки.");
//            }
//        }

//        /// <inheritdoc/>
//        public async Task SetPriceAsync(Nozzle nozzle, decimal? price = null)
//        {
//            try
//            {
//                // Приостанавливаем цикл опроса статуса
//                await PauseStatusLoopAsync();

//                var request = new byte[14];
//                request[0] = 0xA5;
//                request[1] = (byte)(GetNozzleAddress(nozzle) << 4 | _address);
//                request[2] = (byte)Command.ChangePrice;
//                request = AddPriceBytes(request, price == null ? nozzle.Price : price.Value);
//                request[13] = ChecksumHelper.CalculateChecksum(request);

//                await SendAndParseAsync(request);
//            }
//            catch (Exception ex)
//            {
//                _logger.Error(ex, "Ошибка при установке цены на колонке [{ColumnId}].", nozzle.Tube);
//            }
//            finally
//            {
//                // Возобновляем цикл опроса статуса
//                ResumeStatusLoop();
//            }
//        }

//        /// <inheritdoc/>
//        public async Task StartRefuelingQuantityAsync(Nozzle nozzle, decimal? quantity = null)
//        {
//            try
//            {
//                _selectedNozzle = nozzle;
//                // Приостанавливаем цикл опроса статуса
//                await PauseStatusLoopAsync();

//                var request = new byte[14];
//                request[0] = 0xA5;
//                request[1] = (byte)(GetNozzleAddress(_selectedNozzle) << 4 | _address);
//                request[2] = (byte)Command.StartFillingQuantity;
//                request = AddSumBytes(request, quantity == null ? _selectedNozzle.Quantity : quantity.Value);
//                request[13] = ChecksumHelper.CalculateChecksum(request);

//                await SendAndParseAsync(request);
//            }
//            catch (Exception ex)
//            {
//                _logger.Error(ex, "Ошибка при начале заправки на колонке [{ColumnId}].", _selectedNozzle.Tube);
//            }
//            finally
//            {
//                // Возобновляем цикл опроса статуса
//                ResumeStatusLoop();
//            }
//        }

//        /// <inheritdoc/>
//        public async Task StartRefuelingSumAsync(Nozzle nozzle, decimal? sum = null)
//        {
//            try
//            {
//                _selectedNozzle = nozzle;
//                // Приостанавливаем цикл опроса статуса
//                await PauseStatusLoopAsync();

//                var request = new byte[14];
//                request[0] = 0xA5;
//                request[1] = (byte)(GetNozzleAddress(_selectedNozzle) << 4 | _address);
//                request[2] = (byte)Command.StartFillingSum;
//                request = AddSumBytes(request, sum == null ? _selectedNozzle.FuelSale.Sum : sum.Value);
//                request[13] = ChecksumHelper.CalculateChecksum(request);

//                await SendAndParseAsync(request);
//            }
//            catch (Exception ex)
//            {
//                _logger.Error(ex, "Ошибка при начале заправки на колонке [{ColumnId}].", _selectedNozzle.Tube);
//            }
//            finally
//            {
//                // Возобновляем цикл опроса статуса
//                ResumeStatusLoop();
//            }
//        }

//        /// <inheritdoc/>
//        public async Task StopRefuelingAsync(Nozzle nozzle)
//        {
//            try
//            {
//                _selectedNozzle = nozzle;
//                // Приостанавливаем цикл опроса статуса
//                await PauseStatusLoopAsync();

//                var request = new byte[14];
//                request[0] = 0xA5;
//                request[1] = (byte)(GetNozzleAddress(_selectedNozzle) << 4 | _address);
//                request[2] = (byte)Command.StopFilling;
//                request[13] = ChecksumHelper.CalculateChecksum(request);
//                await SendAndParseAsync(request);
//            }
//            catch (Exception ex)
//            {
//                _logger.Error(ex, "Ошибка при остановке заправки на колонке [{ColumnId}].", _selectedNozzle.Tube);
//            }
//            finally
//            {
//                // Возобновляем цикл опроса статуса
//                ResumeStatusLoop();
//            }
//        }

//        /// <inheritdoc/>
//        public async Task ContinueFillingAsync(int tube)
//        {
//            try
//            {
//                // Приостанавливаем цикл опроса статуса
//                await PauseStatusLoopAsync();

//                // Получаем колонку по идентификатору
//                Nozzle? nozzle = GetNozzleByTube(tube);

//                if (nozzle == null)
//                {
//                    return;
//                }

//                // Формируем запрос на продолжение заправки
//                var request = new byte[14];
//                request[0] = 0xA5;
//                request[1] = (byte)(GetNozzleAddress(nozzle) << 4 | _address);
//                request[2] = (byte)Command.ContinueFilling;
//                request[13] = ChecksumHelper.CalculateChecksum(request);

//                // Отправляем запрос и обрабатываем ответ
//                await SendAndParseAsync(request);
//            }
//            catch (Exception ex)
//            {
//                _logger.Error(ex, "Ошибка при продолжении заправки на колонке [{ColumnId}].", tube);
//            }
//            finally
//            {
//                // Возобновляем цикл опроса статуса
//                ResumeStatusLoop();
//            }
//        }

//        /// <inheritdoc/>
//        public async Task CompleteFillingAsync(int tube)
//        {
//            try
//            {
//                // Приостанавливаем цикл опроса статуса
//                await PauseStatusLoopAsync();

//                // Получаем колонку по идентификатору
//                Nozzle? nozzle = GetNozzleByTube(tube);

//                if (nozzle == null)
//                {
//                    return;
//                }

//                // Формируем запрос на продолжение заправки
//                var request = new byte[14];
//                request[0] = 0xA5;
//                request[1] = (byte)(GetNozzleAddress(nozzle) << 4 | _address);
//                request[2] = (byte)Command.CompleteFilling;
//                request[13] = ChecksumHelper.CalculateChecksum(request);

//                // Отправляем запрос и обрабатываем ответ
//                await SendAndParseAsync(request);
//            }
//            catch (Exception ex)
//            {
//                _logger.Error(ex, "Ошибка при завершении заправки на колонке [{ColumnId}].", tube);
//            }
//            finally
//            {
//                // Возобновляем цикл опроса статуса
//                ResumeStatusLoop();
//            }
//        }

//        /// <inheritdoc/>
//        public void StartStatusLoopAsync(TimeSpan interval)
//        {
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
//                _logger.Information("Начат фоновый цикл опроса статуса с интервалом {Interval} мс.", interval.TotalMilliseconds);
//#endif
//                while (!_statusLoopCts.IsCancellationRequested)
//                {
//                    bool semaphoreAcquired = false;
//                    try
//                    {
//                        // Ожидаем освобождения семафора
//                        await _statusLoopSemaphore.WaitAsync(_statusLoopCts.Token);
//                        semaphoreAcquired = true;

//                        // Отправляем запрос на статус
//                        var response = await SendAndParseAsync(GetStatusBytes(), writeTimeout: (int)interval.TotalMilliseconds);
//                    }
//                    catch (TaskCanceledException)
//                    {
//                        break; // Выходим из цикла при отмене
//                    }
//                    catch (Exception ex)
//                    {
//                        SafeInvokeConnectionLost();
//#if DEBUG
//                        _logger.Error(ex, "Ошибка при периодическом опросе статуса. {message}", ex.Message);
//#endif
//                        // при желании здесь можно продолжить цикл
//                    }
//                    finally
//                    {
//                        if (semaphoreAcquired)
//                        {
//                            _statusLoopSemaphore.Release();
//                        }
//                    }

//                    // Ждём заданный интервал
//                    try
//                    {
//                        await Task.Delay(interval, _statusLoopCts.Token);
//                    }
//                    catch (TaskCanceledException)
//                    {
//                        // если токен отменён — выходим из цикла
//                        break;
//                    }

//                    if (_lanfengControllerType == LanfengControllerType.None)
//                    {
//                        await GetFirmwareVersionAsync();
//                    }

//                }

//#if DEBUG
//                _logger.Information("Фоновый цикл опроса статуса остановлен.");
//#endif
//            });
//        }

//        /// <inheritdoc/>
//        public void StopStatusLoop()
//        {
//            if (_statusLoopCts == null)
//            {
//                #if DEBUG
//                _logger.Warning("Цикл статуса не был запущен, вызов StopStatusLoop игнорируется.");
//                #endif
//                return;
//            }

//#if DEBUG
//            _logger.Information("Останавливаем фоновый цикл опроса статуса...");
//#endif
//            _statusLoopCts.Cancel(); // посылаем сигнал на останов

//            // Сбрасываем поля
//            _statusLoopCts = null;
//            _statusLoopTask = null;
//        }

//        #endregion

//        #region Private Voids

//        private byte[] GetStatusBytes()
//        {
//            byte[] data = new byte[14];
//            data[0] = 0xA5;

//            data[1] = (byte)(0 << 4 | _address);

//            data[2] = (byte)Command.Status;

//            data[13] = ChecksumHelper.CalculateChecksum(data);
//            return data;
//        }

//        /// <summary>
//        /// Возвращает адрес колонки.
//        /// </summary>
//        private int GetNozzleAddress(Nozzle nozzle)
//        {
//            // Если однорукавный то адрес пистолета всегда равно 0
//            if (_lanfengControllerType == LanfengControllerType.Single)
//            {
//                return 0;
//            }

//            if (nozzle.Number == 3)
//            {
//                return 4;
//            }
//            if (nozzle.Number == 4)
//            {
//                return 8;
//            }
//            return nozzle.Number;
//        }

//        /// <summary>
//        /// Парсит статус колонки.
//        /// </summary>
//        /// <param name="statusByte">Байт</param>
//        /// <returns></returns>
//        private (int nozzleAddress, NozzleStatus status) ParseStatus(byte statusByte)
//        {
//            return (
//                        (statusByte & 0xF0) >> 4,    // Номер колонки (старшие 4 бита)
//                        (NozzleStatus)(statusByte & 0x0F) // Статус (младшие 4 бита)
//                    );
//        }

//        /// <summary>
//        /// Проверяет целостность (длина, первый байт, CRC) и парсит массива ответа.
//        /// Бросает Exception при ошибке.
//        /// </summary>
//        private byte[] ValidateResponse(byte[] response)
//        {
//            // 1. Проверка длины
//            if (response == null || response.Length < 14)
//            {
//                throw new InvalidOperationException(
//                    $"Недостаточная длина ответа (ожидалось 14 байт, получено {response?.Length ?? 0}). " +
//                    $"Ответ: {BitConverter.ToString(response ?? Array.Empty<byte>())}");
//            }

//            // 2. Первый байт должен быть 0x5A
//            if (response[0] != 0x5A)
//            {
//                response = OrderBytes(response);
//            }

//            // 3. Проверка контрольной суммы: предположим, последний байт - response[13]
//            //   и алгоритм такой же, как у GetStatusBytes (ChecksumHelper.CalculateChecksum).
//            byte calculatedChecksum = ChecksumHelper.CalculateChecksum(response);
//            byte actualChecksum = response[13];
//            if (calculatedChecksum != actualChecksum)
//            {
//                throw new InvalidOperationException(
//                    $"Неверная контрольная сумма. Расчитанная = 0x{calculatedChecksum:X2}, " +
//                    $"в пакете = 0x{actualChecksum:X2}. Ответ: {BitConverter.ToString(response)}");
//            }

//            // 4. Разбор второго байта
//            //    Старшие 4 бита = nozzle, Младшие 4 бита = address
//            byte secondByte = response[1];
//            byte addressNibble = (byte)(secondByte & 0x0F);  // младший nibble

//            // Проверяем, совпадает ли addressNibble с _address
//            if (addressNibble != _address)
//            {
//                throw new InvalidOperationException(
//                    $"Ответ пришёл не с того адреса (ожидали {_address}, получили {addressNibble})." +
//                    $"Ответ: {BitConverter.ToString(response)}");
//            }
//            return response;
//        }

//        /// <summary>
//        /// Парсит ответ от колонки.
//        /// </summary>
//        /// <param name="response"></param>
//        private void ParseRespone(byte[] response)
//        {
//            Status = (NozzleStatus)(byte)(response[11] & 0x0F); // младшие 4 бита
//        }

//        /// <summary>
//        /// Получает версию прошивки.
//        /// </summary>
//        /// <returns></returns>
//        private async Task GetFirmwareVersionAsync()
//        {
//            //A5 00 1A 00 00 00 00 00 00 00 00 00 00 E6
//            var request = new byte[14];
//            request[0] = 0xA5;
//            request[1] = (byte)(0 << 4 | _address);
//            request[2] = (byte)Command.FirmwareVersion;
//            request[13] = ChecksumHelper.CalculateChecksum(request);

//            _ = await SendAndParseAsync(request);
//        }

//        /// <summary>
//        /// Получает счетчик литров.
//        /// </summary>
//        /// <returns></returns>
//        private async Task GetCounterLiterAsync(Nozzle nozzle)
//        {
//            //A5 00 1A 00 00 00 00 00 00 00 00 00 00 E6
//            var request = new byte[14];
//            request[0] = 0xA5;
//            request[1] = (byte)(GetNozzleAddress(nozzle) << 4 | _address);
//            request[2] = (byte)Command.CounterLiter;
//            request[13] = ChecksumHelper.CalculateChecksum(request);

//            _ = await SendAndParseAsync(request);
//        }

//        /// <summary>
//        /// Программное управление.
//        /// </summary>
//        /// <returns></returns>
//        private async Task ProgramControlAsync()
//        {
//            //A5 00 1A 00 00 00 00 00 00 00 00 00 00 E6
//            var request = new byte[14];
//            request[0] = 0xA5;
//            request[1] = (byte)(0 << 4 | _address);
//            request[2] = (byte)Command.ProgramControlMode;
//            request[13] = ChecksumHelper.CalculateChecksum(request);

//            await SendAndParseAsync(request);
//        }

//        /// <summary>
//        /// Отправляет запрос и парсит ответ.
//        /// </summary>
//        /// <param name="request"></param>
//        /// <param name="logPrefix"></param>
//        /// <returns></returns>
//        private async Task<byte[]> SendAndParseAsync(byte[] request, string? logPrefix = null, int writeTimeout = 300)
//        {
//            try
//            {
//                // Логируем исходящий пакет
//                _logger.Information($"{logPrefix} [Tx] {BitConverter.ToString(request)}");

//                // Посылаем и получаем ответ
//                var response = await _sharedSerialPortService.WriteReadAsync(request, 14, writeTimeout: writeTimeout);

//                await ProcessResponse(response, logPrefix);

//                return response;
//            }
//            catch (Exception ex)
//            {
//                _logger.Error(ex, "Ошибка при отправке запроса на колонку.");
//                SafeInvokeConnectionLost();
//                return Array.Empty<byte>();
//            }
//        }

//        private void SafeInvokeConnectionLost()
//        {
//            if ((DateTime.Now - _lastConnectionLost).TotalSeconds < 5) return;
//            _lastConnectionLost = DateTime.Now;
//            try { OnConnectionLost?.Invoke(); }
//            catch (Exception ex) { _logger?.Error(ex, "OnConnectionLost handler failed"); }
//        }

//        private async Task GetStatusAsync()
//        {
//            var request = GetStatusBytes();

//            await SendAndParseAsync(request);
//        }

//        private async Task InitializeByNozzles()
//        {
//            foreach (var nozzle in _nozzles)
//            {
//                // Получает счетчик литров.
//                await GetCounterLiterAsync(nozzle);
//                await Task.Delay(300);
//            }
//        }

//        private Nozzle? GetNozzleByAddress(int address)
//        {
//            if (_lanfengControllerType == LanfengControllerType.Single)
//            {
//                return _nozzles[0];
//            }

//            if (address == 4)
//            {
//                return _nozzles.FirstOrDefault(c => c.Number == 3);
//            }

//            if (address == 8)
//            {
//                return _nozzles.FirstOrDefault(c => c.Number == 4);
//            }

//            return _nozzles.FirstOrDefault(c => c.Number == address);
//        }

//        private Nozzle? GetNozzleByTube(int id)
//        {
//            return _nozzles.FirstOrDefault(c => c.Tube == id);
//        }

//        #endregion

//        #region Process Response

//        /// <summary>
//        /// Обрабатывает входящий массив байтов (ответ от колонки), 
//        /// проверяет валидность и распределяет логику по командам.
//        /// </summary>
//        private async Task ProcessResponse(byte[] response, string? logPrefix = null)
//        {
//            // 1. Общая валидация (длина, 0x5A, CRC и т.д.)
//            var validatedResponse = ValidateResponse(response);

//            // Логируем ответ
//            _logger.Information($"{logPrefix} [Rx] {BitConverter.ToString(validatedResponse)}");

//            // 2. Извлекаем команду (третий байт)
//            var command = GetCommand(validatedResponse[2]);

//            // 3. Парсим статус
//            var (columnNumber, status) = ParseStatus(validatedResponse[11]);

//            var nozzle = GetNozzleByAddress(columnNumber);
            
//            if (nozzle != null)
//            {
//                nozzle.Status = status;
//                Status = status;
//                SetRedyStatusForAllNozzle();
//            }

//            // 4. Обработка команд
//            ProcessCommand(command, validatedResponse);

//            // 5. Обработка статуса
//            await ProcessStatusAsync(status, validatedResponse);

//            // 6. Проверка пистолет поднять или нет
//            HandleColumnLifted(validatedResponse);
//        }

//        private void SetRedyStatusForAllNozzle()
//        {
//            try
//            {
//                if (Status == NozzleStatus.Ready)
//                {
//                    foreach (var item in _nozzles)
//                    {
//                        item.Status = NozzleStatus.Ready;
//                    }
//                }
//            }
//            catch (Exception e)
//            {
//                _logger.Error(e, e.Message);
//            }
//        }

//        private void ProcessCommand(Command command, byte[] response)
//        {
//            switch (command)
//            {
//                case Command.FirmwareVersion:
//                    HandleFirmwareVersion(response);
//                    break;

//                case Command.CounterLiter:
//                    HandleCounterLiter(response);
//                    break;

//                case Command.Status:
//                    HandleStatus(response);
//                    break;

//                case Command.CompleteFilling:
//                    HandleFillingQuantity(response);
//                    int columnNumber = (response[11] & 0xF0) >> 4;
//                    Nozzle? nozzle = GetNozzleByAddress(columnNumber);
//                    if (nozzle is not null)
//                    {
//                        OnCompletedFilling?.Invoke(nozzle.Id);
//                    }
//                    break;

//                default:
//                    break;
//            }
//        }

//        private async Task ProcessStatusAsync(NozzleStatus status, byte[] response)
//        {
//            switch (status)
//            {
//                case NozzleStatus.PumpStop:
//                    await CompleteFilling(response);
//                    break;

//                case NozzleStatus.PumpWorking:
//                    HandleFillingQuantity(response);
//                    break;

//                case NozzleStatus.WaitingRemoved:
//                    HandleWaitingRemove(response);
//                    break;
//            }
//        }

//        /// <summary>
//        /// Обработка команды FirmwareVersion
//        /// </summary>
//        private void HandleFirmwareVersion(byte[] response)
//        {
//            // Пример: response[3] – тип контроллера
//            _lanfengControllerType = (LanfengControllerType)response[3];
//        }

//        private void HandleWaitingRemove(byte[] response)
//        {
//            int columnNumber = (response[11] & 0xF0) >> 4;
//            Nozzle? nozzle = GetNozzleByAddress(columnNumber);

//            if (nozzle is null)
//            {
//                return;
//            }

//            OnWaitingRemoved?.Invoke(nozzle.Id);
//        }

//        /// <summary>
//        /// Обработка команды CounterLiter
//        /// </summary>
//        private void HandleCounterLiter(byte[] response)
//        {
//            // Например, берём 4 байта [7..10] как ASCII-цифры
//            byte[] quantityBytes = { response[7], response[8], response[9], response[10] };
//            decimal quantity = int.Parse(BitConverter.ToString(quantityBytes).Replace("-", "")) / 100.0m;

//            // Старший полубайт байта response[1] — адрес
//            int address = (response[1] & 0xF0) >> 4;

//            Nozzle? nozzle = GetNozzleByAddress(address);

//            if (nozzle is null)
//            {
//                return;
//            }

//            // Вызываем событие
//            if (!_lastCounter.TryGetValue(nozzle.Id, out var prevQ) || prevQ != quantity)
//            {
//                _lastCounter[nozzle.Id] = quantity;
//                nozzle.LastCounter = quantity;
//            }
//        }

//        /// <summary>
//        /// Обработка команды Status
//        /// </summary>
//        private void HandleStatus(byte[] response)
//        {
//            // Часто статус кодируется в каком-то байте, напр. [11]
//            var parsedStatus = (NozzleStatus)(response[11] & 0x0F);

//            Status = parsedStatus; // Установка в свойство Status
//        }

//        /// <summary>
//        /// Обработка команды FillingQuantity
//        /// </summary>
//        private void HandleFillingQuantity(byte[] response)
//        {
//            try
//            {
//                if (_selectedNozzle is null)
//                {
//                    return;
//                }

//                _selectedNozzle.ReceivedQuantity = ParseBytesToDouble(response.AsSpan(7, 4));
//                _selectedNozzle.ReceivedSum = ParseBytesToDecimal(response.AsSpan(3, 4));

//                if (_selectedNozzle.FuelSale != null)
//                {
//                    _selectedNozzle.FuelSale.ReceivedQuantity = _selectedNozzle.ReceivedQuantity;
//                    _selectedNozzle.FuelSale.ReceivedSum = _selectedNozzle.ReceivedSum;
//                    _ = _fuelSaleService.EnqueueUpdateAsync(_selectedNozzle.FuelSale);
//                }
//            }
//            catch (Exception e)
//            {
//                _logger.Error("Ошибка в обработчике команды FillingQuantity", e);
//            }
//        }

//        /// <summary>
//        /// Обработка команды ColumnLifted
//        /// </summary>
//        private void HandleColumnLifted(byte[] response)
//        {
//            int liftedAddress = response[12] & 0x0F;

//            foreach (var nozzle in _nozzles)
//            {
//                bool isLifted = nozzle.Number == liftedAddress;

//                // Обновляем только если значение реально изменилось
//                if (nozzle.Lifted != isLifted)
//                {
//                    nozzle.Lifted = isLifted;
//                }
//            }
//        }

//        private async Task CompleteFilling(byte[] response)
//        {
//            var columnAddress = (response[12] & 0x0F);
//            Nozzle? nozzle = GetNozzleByAddress(columnAddress);

//            if (nozzle is null)
//            {
//                return;
//            }

//            var request = new byte[14];
//            request[0] = 0xA5;
//            request[1] = (byte)(GetNozzleAddress(nozzle) << 4 | _address);
//            request[2] = (byte)Command.CompleteFilling;
//            request[13] = ChecksumHelper.CalculateChecksum(request);
//            await SendAndParseAsync(request);
//        }

//        #endregion

//        #region StartStopStatusLoopAsync

//        private async Task PauseStatusLoopAsync()
//        {
//            if (_isStatusLoopPaused)
//            {
//#if DEBUG
//                _logger.Warning("Опрос статуса уже приостановлен.");
//#endif
//                return;
//            }

//            await _statusLoopSemaphore.WaitAsync();
//            _isStatusLoopPaused = true;
//#if DEBUG
//            _logger.Information("Опрос статуса приостановлен.");
//#endif
//        }

//        private void ResumeStatusLoop()
//        {
//            if (!_isStatusLoopPaused)
//            {
//#if DEBUG
//                _logger.Warning("Опрос статуса не приостановлен.");
//#endif
//                return;
//            }

//            _statusLoopSemaphore.Release();
//            _isStatusLoopPaused = false;
//#if DEBUG
//            _logger.Information("Опрос статуса возобновлён.");
//#endif
//        }

//        #endregion

//        #region Bytes

//        /// <summary>
//        /// Добавляет цену в массив байтов.
//        /// </summary>
//        private byte[] AddPriceBytes(byte[] response, decimal value)
//        {
//            // 1) Умножаем на 100, чтобы убрать десятичную часть
//            //    12.34 => 1234
//            int intValue = (int)(value * 100);

//            // 2) Преобразуем в строку
//            //    1234 => "1234"
//            string strValue = intValue.ToString();

//            // 3) Дополняем до 6 символов слева '0'
//            //    "1234" => "001234"
//            strValue = strValue.PadLeft(6, '0');

//            // 4) Разбиваем на 3 пары по 2 символа и парсим как hex
//            //    "00" => 0x00, "12" => 0x12, "34" => 0x34
//            byte[] result = Enumerable.Range(0, 3)
//                                      .Select(i => strValue.Substring(i * 2, 2))   // "00", "12", "34"
//                                      .Select(hex => Convert.ToByte(hex, 16))
//                                      .ToArray();

//            response[3] = result[0];
//            response[4] = result[1];
//            response[5] = result[2];

//            return response;
//        }

//        private byte[] AddSumBytes(byte[] response, decimal sum)
//        {
//            // 1) Умножаем на 100, чтобы убрать десятичную часть
//            //    12.34 => 1234
//            int intValue = (int)(sum * 100);

//            // 2) Преобразуем в строку
//            //    1234 => "1234"
//            string strValue = intValue.ToString();

//            // 3) Дополняем до 8 символов слева '0'
//            //    "1234" => "00001234"
//            strValue = strValue.PadLeft(8, '0');

//            // 4) Разбиваем на 4 пары по 2 символа и парсим как hex
//            //    "00" => 0x00, "00" => 0x00, "12" => 0x12, "34" => 0x34
//            byte[] result = Enumerable.Range(0, 4)
//                                      .Select(i => strValue.Substring(i * 2, 2))   // "00", "12", "34"
//                                      .Select(hex => Convert.ToByte(hex, 16))
//                                      .ToArray();

//            response[3] = result[0];
//            response[4] = result[1];
//            response[5] = result[2];
//            response[6] = result[3];

//            return response;
//        }

//        /// <summary>
//        /// Переставляет байты в массиве так, чтобы первый байт был 0x5A.
//        /// </summary>
//        /// <param name="bytes"></param>
//        /// <returns></returns>
//        /// <exception cref="InvalidOperationException"></exception>
//        private byte[] OrderBytes(byte[] bytes)
//        {
//            string dsdf = BitConverter.ToString(bytes);
//            // Создаем новый массив и копируем байты после 0x5A в начало массива
//            byte[] orderedBytes = new byte[bytes.Length];

//            int index = Array.IndexOf(bytes, (byte)0x5A);
//            byte firstByte = bytes[0];

//            if (index == -1)
//            {
//                throw new InvalidOperationException("В массиве не найден байт 5A. Невозможно отсортировать.");
//            }

//            if (firstByte != 0x5A)
//            {
//                // Ищем индекс первого байта 0x5A
//                int startIndex = Array.IndexOf(bytes, (byte)0x5A);

//                // Если байт не найден, возвращаем исходный массив
//                if (startIndex < 0)
//                {
//                    return bytes;
//                }

//                Array.Copy(bytes, startIndex, orderedBytes, 0, bytes.Length - startIndex);

//                // Копируем байты перед 0x5A в конец массива
//                Array.Copy(bytes, 0, orderedBytes, bytes.Length - startIndex, startIndex);

//                string dsdasdasf = BitConverter.ToString(orderedBytes);

//                return orderedBytes;
//            }

            

//            return bytes;
//        }

//        /// <summary>
//        /// Парсит 4 байта в double. Количество литров.
//        /// </summary>
//        private decimal ParseBytesToDouble(Span<byte> bytes)
//        {
//            return int.Parse(BitConverter.ToString(bytes.ToArray()).Replace("-", "")) / 100.0m;
//        }

//        /// <summary>
//        /// Парсит 4 байта в decimal. Сумма.
//        /// </summary>
//        private decimal ParseBytesToDecimal(Span<byte> bytes)
//        {
//            return int.Parse(BitConverter.ToString(bytes.ToArray()).Replace("-", "")) / 100.0m;
//        }

//        private Command GetCommand(byte response)
//        {
//            return (Command)response;
//        }

//        #endregion

//        #region PropertyChanged

//        public event PropertyChangedEventHandler? PropertyChanged;

//        protected void OnPropertyChanged(string propertyName)
//        {
//            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
//        }

//        #endregion

//        #region Logs

//        /// <summary>
//        /// Инициализация логгера.
//        /// </summary>
//        private void InitLog(int side)
//        {
//            // 1. Создадим/убедимся, что существует папка logs
//            var logsDir = Path.Combine(AppContext.BaseDirectory, "logs");
//            Directory.CreateDirectory(logsDir);

//            // 2. Формируем имя файла. Можно добавить время, 
//            //    но обязательно без «:» (двоеточий). Например, yyyy-MM-dd_HH-mm-ss.
//            var logFilePath = Path.Combine(logsDir, $"{nameof(LanfengFuelDispenser)}_{side}_{DateTime.Now:yyyyddMM}.log");

//            // 3. Настраиваем Serilog
//            _logger = new LoggerConfiguration()
//                // Указываем минимальный уровень
//                .MinimumLevel.Debug()
//                // Пишем в файл с «дневным» ротационным интервалом
//                .WriteTo.File(
//                    path: logFilePath,
//                    rollingInterval: RollingInterval.Day,
//                    // Можно задать, сколько файлов хранить
//                    retainedFileCountLimit: 7,
//                    // Можно включить автопереход на новый файл при достижении лимита размера
//                    rollOnFileSizeLimit: true
//                )
//                // При желании можно добавить вывод в консоль
//                //.WriteTo.Console()
//                .CreateLogger();

//            // 4. Пробный лог на уровне Information
//            _logger.Information($"---------------Ланфенг инициализирован. [{DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.fff")}]---------------");
//        }

//        #endregion

//        #region Dispose

//        public void Dispose()
//        {
//            // 1. Остановить фоновый цикл
//            StopStatusLoop();
//            _statusLoopTask?.GetAwaiter().GetResult();

//            // 2. Освободить семафор и CTS
//            _statusLoopSemaphore?.Dispose();
//            _statusLoopCts?.Dispose();

//            // 3. Закрыть/освободить порт
//            _sharedSerialPortService?.Dispose();

//            _nozzles = null;
//            _columns = null;
//        }

//        #endregion
    }
}
