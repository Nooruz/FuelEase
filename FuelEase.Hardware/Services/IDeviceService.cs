using System.Threading.Tasks;

namespace FuelEase.Hardware.Services
{
    public interface IDeviceService<TDevice>
    {
        Task SaveDeviceAsync(TDevice device);
    }
}
