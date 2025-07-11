namespace KIT.GasStation.HardwareConfigurations.Services
{
    public class PortManager : IPortManager
    {
        #region Private Members

        private readonly Dictionary<(string portName, int baudRate), ISharedSerialPortService> _ports = new();
        private readonly object _lock = new object();

        #endregion

        public async Task<ISharedSerialPortService> GetPortServiceAsync(string portName, int baudRate)
        {
            var key = (portName, baudRate);

            lock (_lock)
            {
                if (_ports.TryGetValue(key, out var existingService))
                {
                    // Порт уже открыт, возвращаем готовый сервис
                    return existingService;
                }
            }

            // Если ключа нет, создаём новый сервис
            var newService = new SharedSerialPortService();
            // Предположим, что SharedSerialPortService.OpenAsync(...) действительно асинхронный
            await newService.OpenAsync(portName, baudRate);

            // Записываем в словарь внутри lock
            lock (_lock)
            {
                if (!_ports.ContainsKey(key))
                {
                    _ports[key] = newService;
                }
                else
                {
                    // Если кто-то зашёл «между» нашим lock, придётся закрыть наш только что открытый сервис, 
                    // чтобы избежать дублирования.
                    newService.Dispose();
                    // Или await newService.CloseAsync(); – зависит от вашей реализации
                }
                return _ports[key];
            }
        }

        public void ClosePortService(string portName, int baudRate)
        {
            var key = (portName, baudRate);

            ISharedSerialPortService serviceToClose = null;
            lock (_lock)
            {
                if (_ports.TryGetValue(key, out var existingService))
                {
                    serviceToClose = existingService;
                    _ports.Remove(key);
                }
            }

            // Если действительно есть, что закрывать, освобождаем ресурсы
            // Предположим, что у SharedSerialPortService есть асинхронный метод CloseAsync()
            serviceToClose?.Close();
        }
    }
}
