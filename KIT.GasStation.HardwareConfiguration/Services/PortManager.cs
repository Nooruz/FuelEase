using System.Collections.Concurrent;
using System.IO.Ports;

namespace KIT.GasStation.HardwareConfigurations.Services
{
    /// <summary>
    /// Менеджер портов: выдаёт lease (аренду) на общий порт.
    /// Гарантирует одиночное открытие (Lazy<Task<...>>) и закрытие при отсутствии арендаторов.
    /// </summary>
    public sealed class PortManager : IPortManager
    {
        #region Private Members

        private readonly ConcurrentDictionary<PortKey, Entry> _map = new();

        #endregion

        public async Task<PortLease> AcquireAsync(PortKey key, SerialPortOptions options, CancellationToken ct)
        {
            // Создаём ленивое открытие порта РОВНО один раз с ExecutionAndPublication
            var entry = _map.GetOrAdd(key, _ =>
            {
                return new Entry(new Lazy<Task<ISharedSerialPortService>>(async () =>
                {
                    var svc = new SharedSerialPortService();
                    try
                    {
                        await svc.OpenAsync(key, options, ct);
                        return svc;
                    }
                    catch
                    {
                        // Если открытие провалилось — удаляем ключ, чтобы не залипло в сломанном состоянии
                        _map.TryRemove(key, out Entry _);
                        await svc.DisposeAsync();
                        throw;
                    }
                }, isThreadSafe: true));
            });

            // Дожидаемся реального открытия порта (или ошибки открытия)
            var port = await entry.LazyService.Value;

            // Увеличиваем счётчик ссылок
            Interlocked.Increment(ref entry.RefCount);

            // Возвращаем lease, который при Dispose уменьшит счётчик и при нуле закроет порт
            return new PortLease(port, async () =>
            {
                if (Interlocked.Decrement(ref entry.RefCount) == 0)
                {
                    // При нуле — закрываем сервис и удаляем запись
                    try
                    {
                        await port.CloseAsync();
                        await port.DisposeAsync();
                    }
                    finally
                    {
                        _map.TryRemove(key, out _);
                    }
                }
            });
        }

        /// <summary>
        /// Явно закрыть порт, если никто им не пользуется (RefCount==0).
        /// Полезно для уборки простаивающих портов по таймеру.
        /// </summary>
        public async Task CloseIfIdleAsync(PortKey key)
        {
            if (_map.TryGetValue(key, out var entry) && entry.RefCount == 0)
            {
                if (entry.LazyService.IsValueCreated)
                {
                    var svc = await entry.LazyService.Value;
                    await svc.CloseAsync();
                    await svc.DisposeAsync();
                }
                _map.TryRemove(key, out _);
            }
        }

        // Внутренняя запись: ленивое создание сервиса + счётчик ссылок
        private sealed class Entry
        {
            public Lazy<Task<ISharedSerialPortService>> LazyService { get; }
            public int RefCount;

            public Entry(Lazy<Task<ISharedSerialPortService>> lazy) => LazyService = lazy;
        }
    }
}
