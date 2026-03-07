namespace KIT.App.Infrastructure.Services
{
    public interface IDeviceService<TDevice>
    {
        Task SaveDeviceAsync(TDevice device);
    }
}
