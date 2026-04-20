namespace KIT.GasStation.HardwareSettings.Services
{
    public interface IDeviceService<TDevice>
    {
        Task SaveDeviceAsync(TDevice device);
    }
}
