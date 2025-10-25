using KIT.GasStation.HardwareConfigurations.Exceptions;
using Microsoft.Extensions.Logging;
using System.IO.Ports;

namespace KIT.GasStation.HardwareConfigurations.Services
{
    /// <summary>
    /// Реализация интерфейса ISerialPortStreamState для управления COM-портом.
    /// </summary>
    public class SharedSerialPortService : ISharedSerialPortService
    {
        #region Private Members

        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly SemaphoreSlim _reconnectLock = new(1, 1);
        private readonly ILogger<SharedSerialPortService> _logger;
        private string? _portName;
        private int _baudRate;
        private CancellationTokenSource? _reconnectCts;
        private Task? _reconnectTask;
        private int _writeCounter = 0;

        #endregion

        #region Public Properties

        public SerialPort Port { get; private set; }

        #endregion

        #region Actions

        /// <inheritdoc/>
        public event Action<byte[]> OnDataReceived;

        #endregion

        #region Constructors

        public SharedSerialPortService()
        {
            // 1. Создадим/убедимся, что существует папка logs
            var logsDir = Path.Combine(AppContext.BaseDirectory, "logs");
            Directory.CreateDirectory(logsDir);

            // 2. Формируем имя файла. Можно добавить время, 
            //    но обязательно без «:» (двоеточий). Например, yyyy-MM-dd_HH-mm-ss.
            var logFilePath = Path.Combine(logsDir, $"{nameof(SharedSerialPortService)}_{DateTime.Now:dd.MM.yyyy}.log");

            //// 3. Настраиваем Serilog
            //_logger = new LoggerConfiguration()
            //    // Указываем минимальный уровень
            //    .MinimumLevel.Debug()
            //    // Пишем в файл с «дневным» ротационным интервалом
            //    .WriteTo.File(
            //        path: logFilePath,
            //        rollingInterval: RollingInterval.Day,
            //        // Можно задать, сколько файлов хранить
            //        retainedFileCountLimit: 7,
            //        // Можно включить автопереход на новый файл при достижении лимита размера
            //        rollOnFileSizeLimit: true
            //    )
            //    // При желании можно добавить вывод в консоль
            //    //.WriteTo.Console()
            //    .CreateLogger();

            //// 4. Пробный лог на уровне Information
            //_logger.Information("Менеджер портов инициализирован. [{Timestamp}]",
            //    DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.fff"));
        }

        #endregion

        #region Public Voids

        /// <inheritdoc/>
        public async Task OpenAsync(string portName, int baudRate, CancellationToken cancellationToken = default)
        {
            try
            {
                _portName = portName;
                _baudRate = baudRate;

                //_logger.Information("Начинается асинхронное открытие порта [{PortName}] со скоростью {BaudRate}.", portName, baudRate);

                await Task.Run(() =>
                {
                    // Если отмена запрошена ещё до начала
                    cancellationToken.ThrowIfCancellationRequested();

                    // Вызываем синхронный Open
                    Open(portName, baudRate);

                    // Ещё одна проверка отмены сразу после открытия
                    cancellationToken.ThrowIfCancellationRequested();
                }, cancellationToken);

                //_logger.Information("Асинхронное открытие порта [{PortName}] завершено (скорость: {BaudRate}).", portName, baudRate);
            }
            catch (Exception ex)
            {
                throw new SharedSerialPortException("Ошибка при открытии порта.", ex);
            }
        }

        /// <inheritdoc/>
        public void Close()
        {
            //_logger.Information("Начинается закрытие порта...");
            try
            {
                Port?.Close();
                Port?.Dispose();

                //_logger.Information("Порт успешно закрыт и освобождён.");
            }
            catch (Exception e)
            {
                //_logger.Error(e, "Ошибка при закрытии порта.");
            }
        }

        /// <inheritdoc/>
        public async Task<byte[]> WriteReadAsync(byte[] bytes, 
            int readBufferLength, 
            int maxRetries = 3,
            int writeToReadDelayMs = 50,
            int readTimeoutMs = 3000)
        {
            if (++_writeCounter % 10_000 == 0)
                await RestartPortAsync();

            await _semaphore.WaitAsync();
            try
            {
                if (Port is null || !Port.IsOpen)
                    throw new InvalidOperationException("Порт не открыт.");

                var attempt = 0;
                while (true)
                {
                    try
                    {
                        // Пишем по-настоящему async. Никаких DiscardOutBuffer!
                        await Port.BaseStream.WriteAsync(bytes, 0, bytes.Length);
                        await Port.BaseStream.FlushAsync();

                        if (writeToReadDelayMs > 0)
                            await Task.Delay(writeToReadDelayMs);

                        var buffer = new byte[readBufferLength];
                        var read = await ReadExactlyAsync(Port, buffer, 0, readBufferLength, readTimeoutMs);

                        // подчистим хвост (остатки сверх ожидаемого)
                        var leftover = Port.BytesToRead;
                        if (leftover > 0) Port.DiscardInBuffer();

                        return buffer;
                    }
                    catch (TimeoutException) when (++attempt <= maxRetries)
                    {
                        // можно добавить небольшой backoff
                        await Task.Delay(50 * attempt);
                        continue;
                    }
                    catch (IOException) when (++attempt <= maxRetries)
                    {
                        await Task.Delay(50 * attempt);
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                if (Port is null || !Port.IsOpen)
                    _ = TriggerReconnectInBackground();

                throw new SharedSerialPortException("Ошибка при записи/чтении данных в/из порта.", ex);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task RestartPortAsync()
        {
            try
            {
                if (Port != null)
                {
                    Port.Close();
                    await Task.Delay(100);
                    Port.Open();
                    //_logger.Information("Порт успешно перезапущен.");
                }
            }
            catch (Exception ex)
            {
                //_logger.Error(ex, "Не удалось перезапустить порт.");
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            //_logger.Information("Вызывается Dispose(). Закрываем порт и освобождаем ресурсы...");

            // Здесь освобождаем ресурсы
            Close();

            // Затем можно попросить GC не вызывать финализатор:
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Private Voids

        /// <summary>
        /// Асинхронно запускает попытку переподключения, синхронизируя доступ с помощью SemaphoreSlim.
        /// Fire-and-forget: задача запускается в фоне и не блокирует вызывающий поток.
        /// </summary>
        private async Task TriggerReconnectInBackground()
        {
            await _reconnectLock.WaitAsync();
            try
            {
                if (_reconnectTask == null || _reconnectTask.IsCompleted)
                {
                    // Запускаем попытку переподключения в фоне
                    _reconnectTask = AttemptReconnectAsync();
                }
            }
            finally
            {
                _reconnectLock.Release();
            }
        }

        /// <summary>
        /// Чтение данных из порта с таймаутом.
        /// </summary>
        private async Task<int> ReadExactlyAsync(SerialPort port, byte[] buffer, int offset, int count, int timeout)
        {
            if (port is null || !port.IsOpen)
                throw new InvalidOperationException("Порт не открыт.");

            var totalRead = 0;
            var sw = System.Diagnostics.Stopwatch.StartNew();

            while (totalRead < count)
            {
                var remaining = count - totalRead;
                var leftMs = timeout - (int)sw.ElapsedMilliseconds;
                if (leftMs <= 0)
                    throw new TimeoutException("Таймаут при чтении из порта.");

                using var cts = new CancellationTokenSource(leftMs);

                // Блокирующее ожидание данных (реально ждёт до leftMs)
                var read = await port.BaseStream.ReadAsync(
                    buffer.AsMemory(offset + totalRead, remaining), cts.Token);

                if (read > 0)
                {
                    totalRead += read;
                    continue;
                }

                // read == 0: данных нет прямо сейчас — крутим цикл до дедлайна
                // (не бросаем IOException преждевременно)
            }

            return totalRead;
        }

        /// <summary>
        /// Открытие порта.
        /// </summary>
        private void Open(string portName, int baudRate)
        {
            try
            {
                //_logger.Information($"Начинаем открытие порта [{portName}] (скорость: {baudRate}).");
                Port = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One)
                {
                    Handshake = Handshake.None,
                    ReadTimeout = 500,
                    WriteTimeout = 500
                };

                if (Port.IsOpen)
                {
                    //_logger.Information($"Порт [{portName}] успешно открыт.");
                }
                else
                {
                    Port.Open();
                    //_logger.Information($"Не удалось открыть порт [{portName}].");
                }
            }
            catch (Exception ex)
            {
                //_logger.Error(ex, $"Ошибка при открытии порта [{portName}].");
            }
        }

        /// <summary>
        /// Попытка переподключения к порту.
        /// </summary>
        private async Task AttemptReconnectAsync()
        {
            if (string.IsNullOrEmpty(_portName) || _baudRate == 0) return;

            using var cts = new CancellationTokenSource();
            _reconnectCts = cts;

            //_logger.Information("Начало переподключения к порту {PortName}", _portName);

            while (!cts.IsCancellationRequested)
            {
                try
                {
                    // Асинхронное закрытие порта
                    if (Port != null && Port.IsOpen)
                        await Task.Run(() => Port.Close());

                    // Асинхронная попытка открытия с задержкой
                    var success = await Task.Run(() =>
                    {
                        try
                        {
                            Open(_portName, _baudRate);
                            return Port?.IsOpen == true;
                        }
                        catch
                        {
                            return false;
                        }
                    });

                    if (success) break;

                    // Задержка между попытками
                    await Task.Delay(1000, cts.Token);
                }
                catch (Exception ex)
                {
                    //_logger.Error(ex, "Ошибка переподключения");
                    await Task.Delay(5000, cts.Token);
                }
            }
            _reconnectCts = null;
        }

        /// <summary>
        /// Проверка статуса порта.
        /// </summary>
        private void CheckPortStatus(SerialPort port)
        {
            if (port == null || !port.IsOpen)
            {
                //_logger.Error("Порт недоступен для чтения");
                throw new InvalidOperationException("Порт не открыт");
            }
        }

        #endregion
    }
}
