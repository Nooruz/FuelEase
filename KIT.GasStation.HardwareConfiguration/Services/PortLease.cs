namespace KIT.GasStation.HardwareConfigurations.Services
{
    /// <summary>
    /// Аренда порта (референс-счётчик). Закрытие лиза уменьшает счётчик — при нуле порт закрывается менеджером.
    /// </summary>
    public sealed class PortLease : IAsyncDisposable
    {
        public ISharedSerialPortService Port { get; }
        private readonly Func<ValueTask> _onDispose;

        public PortLease(ISharedSerialPortService port, Func<ValueTask> onDispose)
            => (Port, _onDispose) = (port, onDispose);

        public ValueTask DisposeAsync() => _onDispose();
    }
}
