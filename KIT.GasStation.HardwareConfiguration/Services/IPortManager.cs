namespace KIT.GasStation.HardwareConfigurations.Services
{
    /// <summary>
    /// Менеджер портов: выдаёт лиз (lease) на общий порт с нужными параметрами.
    /// Гарантирует одиночное открытие и корректное закрытие, когда последний клиент отпустит лиз.
    /// </summary>
    public interface IPortManager
    {
        Task<PortLease> AcquireAsync(PortKey key, SerialPortOptions options, CancellationToken ct);
        Task CloseIfIdleAsync(PortKey key); // опционально вручную закрыть, если ref=0
    }
}
