using System.Threading.Tasks;

namespace KIT.GasStation.Hardware.Services
{
    public interface IDeviceService<TDevice>
    {
        Task SaveDeviceAsync(TDevice device);
    }
}
