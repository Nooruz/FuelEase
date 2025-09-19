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
        public async Task<byte[]> WriteReadAsync(byte[] bytes, int readBufferLength, int maxRetries = 3, int writeTimeout = 200)
        {
            // Watchdog: раз в 10 000 итераций перезапускаем порт
            if (++_writeCounter % 10_000 == 0)
            {
                //_logger.Information("Watchdog: перезапускаем порт после {Count} запросов", _writeCounter);
                await RestartPortAsync();
            }

            //_logger.Information(
            //    "Записываем {BytesLength} байт в порт. Ожидание перед чтением: {WriteTimeout} мс. Буфер для чтения: {ReadBufferLength} байт. Повторений: {MaxRetries}.",
            //    bytes?.Length, writeTimeout, readBufferLength, maxRetries);

            await _semaphore.WaitAsync();
            try
            {
                if (Port == null || !Port.IsOpen)
                {
                    throw new InvalidOperationException("Порт не открыт.");
                }

                // Запись данных в порт
                //_logger.Debug("Отправляем данные в порт...");
                // Асинхронная запись данных с использованием Task.Run
                await Task.Run(() => Port.Write(bytes, 0, bytes.Length));

                // очистить всё, что осталось в исходящем буфере
                Port.DiscardOutBuffer();
                //_logger.Debug("DiscardOutBuffer вызван.");

                // Задержка перед чтением
                //_logger.Debug("Задержка {WriteTimeout} мс перед чтением...", writeTimeout);
                await Task.Delay(writeTimeout);

                // Чтение данных из порта
                byte[] buffer = new byte[readBufferLength];
                //_logger.Debug("Читаем {ReadBufferLength} байт из порта...", readBufferLength);

                // Чтение данных из порта с таймаутом
                await ReadExactlyAsync(Port, buffer, 0, readBufferLength, timeout: 3000);

                // логнуть, сколько байт осталось «за плечами»
                int leftover = Port.BytesToRead;
                //_logger.Debug("BytesToRead перед очисткой: {Leftover}", leftover);

                // очистить всё, что накопилось сверх ожидаемого
                Port.DiscardInBuffer();
                //_logger.Debug("DiscardInBuffer вызван.");

                //await Task.Run(() => Port.Read(buffer, 0, readBufferLength));

                //_logger.Information("Данные успешно прочитаны. Возвращаем {BytesRead} байт.", buffer.Length);

                // Возврат полученных данных
                return buffer;
            }
            catch (Exception ex)
            {
                //_logger.Error(ex, "Ошибка при записи/чтении данных в/из порта.");

                // Если порт закрыт или произошла ошибка, запускаем переподключение в фоне.
                if (Port == null || !Port.IsOpen)
                {
                    _ = TriggerReconnectInBackground();
                }

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
            int totalRead = 0;
            var cts = new CancellationTokenSource();
            var timeoutTask = Task.Delay(timeout, cts.Token);

            //_logger.Debug("Начинаем асинхронное чтение {Count} байт с таймаутом {Timeout} мс.", count, timeout);

            try
            {
                while (totalRead < count)
                {
                    CheckPortStatus(port);

                    var readTask = Task.Run(() =>
                    {
                        var bytesAvailable = port.BytesToRead;
                        if (bytesAvailable == 0) return 0;

                        return port.Read(buffer, offset + totalRead, Math.Min(count - totalRead, bytesAvailable));
                    }, cts.Token);

                    var completedTask = await Task.WhenAny(readTask, timeoutTask);

                    if (completedTask == timeoutTask)
                        throw new TimeoutException("Таймаут при чтении из порта.");

                    var bytesRead = await readTask;
                    if (bytesRead == 0)
                    {
                        //_logger.Error("Соединение закрыто до завершения чтения");
                        throw new IOException("Connection closed before reading all data");
                    }

                    totalRead += bytesRead;
                    //_logger.Debug("Прочитано {BytesRead} байт, всего {TotalRead}/{Count}", bytesRead, totalRead, count);
                }

                return totalRead;
            }
            catch (OperationCanceledException)
            {
                //_logger.Error("Операция чтения была отменена");
                throw;
            }
            catch (Exception ex)
            {
                //_logger.Error(ex, "Ошибка чтения из порта");
                throw;
            }
            finally
            {
                cts.Cancel();
                cts.Dispose();
            }
        }

        /// <summary>
        /// Открытие порта.
        /// </summary>
        private void Open(string portName, int baudRate)
        {
            try
            {
                //_logger.Information($"Начинаем открытие порта [{portName}] (скорость: {baudRate}).");
                Port = new SerialPort(portName, baudRate)
                {
                    ReadTimeout = 1000,
                    WriteTimeout = 1000
                };

                Port.Open();

                if (Port.IsOpen)
                {
                    //_logger.Information($"Порт [{portName}] успешно открыт.");
                }
                else
                {
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
