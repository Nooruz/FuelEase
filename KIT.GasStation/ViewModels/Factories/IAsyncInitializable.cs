using System.Threading.Tasks;

namespace KIT.GasStation.ViewModels.Factories
{
    /// <summary>
    /// Интерфейс для асинхронной инициализации.
    /// </summary>
    public interface IAsyncInitializable
    {
        /// <summary>
        /// Асинхронный метод для инициализации.
        /// </summary>
        Task StartAsync();
    }
}
