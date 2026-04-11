using KIT.GasStation.HardwareConfigurations.Models;
using System.Collections.ObjectModel;

namespace KIT.GasStation.HardwareConfigurations.Services
{
    public interface IHardwareConfigurationService
    {
        /// <summary>
        /// Событие изменения свойств контроллера.
        /// </summary>
        event Action<Controller> OnControllerPropertyChanged;

        /// <summary>
        /// Событие изменения свойств кассы.
        /// </summary>
        event Action<CashRegister> OnCashRegisterPropertyChanged;

        /// <summary>
        /// Читает конфигурацию из файла.
        /// </summary>
        /// <returns>Объект конфигурации.</returns>
        Task<HardwareConfiguration> ReadConfigurationFileAsync();

        /// <summary>
        /// Проверяет существование файла конфигурации и создаёт новый, если его нет.
        /// </summary>
        Task EnsureConfigurationFileExistsAsync();

        /// <summary>
        /// Создаёт файл конфигурации с настройками по умолчанию.
        /// </summary>
        Task CreateDefaultConfigurationFileAsync();

        /// <summary>
        /// Сохраняет конфигурацию в файл.
        /// </summary>
        /// <param name="configuration">Объект конфигурации для сохранения.</param>
        Task SaveConfigurationFileAsync(HardwareConfiguration configuration);

        /// <summary>
        /// Получает коллекцию контроллеров.
        /// </summary>
        /// <returns>Коллекция контроллеров.</returns>
        Task<ObservableCollection<Controller>> GetControllersAsync();

        /// <summary>
        /// Получает коллекци кассовых аппаратов.
        /// </summary>
        /// <returns>Коллекция кассовых аппаратов.</returns>
        Task<ObservableCollection<CashRegister>> GetCashRegistersAsync();

        /// <summary>
        /// Получает коллекцию колонок.
        /// </summary>
        /// <returns>Коллекцтя колонок.</returns>
        Task<ObservableCollection<Column>> GetColumnsAsync();

        /// <summary>
        /// Получает коллекцию колонок по адресу и по идентификатор Controller.
        /// </summary>
        /// <param name="address">Адрес колонки.</param>
        /// <param name="controllerId">Идентификатор Controller.</param>
        /// <returns>Коллекцтя колонок.</returns>
        Task<ObservableCollection<Column>> GetColumnsByControllerIdAndAddressAsync(Guid controllerId, int address);

        /// <summary>
        /// Получает контроллер по идентификатору.
        /// </summary>
        /// <returns>Колонка</returns>
        Task<Column?> GetColumnByIdAsync(Guid id);

        /// <summary>
        /// Получает контроллер по идентификатору.
        /// </summary>
        Task<CashRegister?> GetCashRegisterAsync(Guid id);

        /// <summary>
        /// Добавляет новый контроллер или обновляет существующий в конфигурационном файле.
        /// </summary>
        /// <param name="controller">Объект контроллера для добавления или обновления.</param>
        Task SaveControllerAsync(Controller controller);

        /// <summary>
        /// Сохраняет пистолет
        /// </summary>
        Task SaveColumnAsync(Column column);

        /// <summary>
        /// Добавляет новую кассу или обновляет существующую в конфигурационном файле.
        /// </summary>
        /// <param name="cashRegister">Объект кассы для добавления или обновления.</param>
        Task SaveCashRegisterAsync(CashRegister cashRegister);

        /// <summary>
        /// Удаляет существующий контроллер.
        /// </summary>
        /// <param name="controllerId">Идентификатор контроллера для удаления.</param>
        Task<bool> RemoveControllerAsync(Guid controllerId);

        /// <summary>
        /// Удаляет существующую кассу.
        /// </summary>
        /// <param name="cashRegisterId">Идентификатор кассы для удаления.</param>
        Task<bool> RemoveCashRegisterAsync(Guid cashRegisterId);
    }
}
