using KIT.GasStation.HardwareConfigurations.Models;
using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace KIT.GasStation.HardwareConfigurations.Services
{
    public class HardwareConfigurationService : IHardwareConfigurationService
    {
        #region Private Voids

        private readonly string _filePath;

        #endregion

        #region Actions

        /// <inheritdoc/>
        public event Action<Controller> OnControllerPropertyChanged;

        /// <inheritdoc/>
        public event Action<CashRegister> OnCashRegisterPropertyChanged;

        #endregion

        #region Constructors

        public HardwareConfigurationService()
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "КИТ-АЗС");
            _filePath = Path.Combine(dir, "HardwareConfiguration.xml");
        }

        #endregion

        #region Public Properties

        /// <inheritdoc/>
        public async Task<HardwareConfiguration> ReadConfigurationFileAsync()
        {
            if (!File.Exists(_filePath) || new FileInfo(_filePath).Length == 0)
            {
                throw new InvalidOperationException($"Файл конфигурации отсутствует или пуст. {_filePath}");
            }

            try
            {
                var serializer = new XmlSerializer(typeof(HardwareConfiguration));
                using var reader = new StreamReader(_filePath);
                var config = await Task.Run(() => (HardwareConfiguration?)serializer.Deserialize(reader));

                return config ?? throw new InvalidOperationException("Не удалось десериализовать конфигурацию.");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка при чтении конфигурационного файла: {ex.Message}", ex);
            }
        }

        /// <inheritdoc/>
        public async Task EnsureConfigurationFileExistsAsync()
        {
            try
            {
                if (!File.Exists(_filePath) || new FileInfo(_filePath).Length == 0)
                {
                    await CreateDefaultConfigurationFileAsync();
                }
            }
            catch (Exception)
            {

            }
        }

        /// <inheritdoc/>
        public async Task CreateDefaultConfigurationFileAsync()
        {
            try
            {
                var defaultConfig = new HardwareConfiguration();
                var serializer = new XmlSerializer(typeof(HardwareConfiguration));

                var dir = Path.GetDirectoryName(_filePath);
                if (string.IsNullOrWhiteSpace(dir))
                    throw new InvalidOperationException($"Плохой путь: '{_filePath}'");

                Directory.CreateDirectory(dir); // ключевая строка

                await using var fs = new FileStream(_filePath, FileMode.Create, FileAccess.Write, FileShare.None);
                serializer.Serialize(fs, defaultConfig);
            }
            catch (Exception ex)
            {
                // хотя бы так, чтобы не было "молча обосрался и ушёл"
                throw new IOException($"Не смог создать конфиг по пути: '{_filePath}'", ex);
            }
        }

        /// <inheritdoc/>
        public async Task SaveConfigurationFileAsync(HardwareConfiguration configuration)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(HardwareConfiguration));
                using var writer = new StreamWriter(_filePath);
                await Task.Run(() => serializer.Serialize(writer, configuration));
            }
            catch (Exception)
            {

            }
        }

        /// <inheritdoc/>
        public async Task<ObservableCollection<Controller>> GetControllersAsync()
        {
            var configuration = await ReadConfigurationFileAsync();

            foreach (var controller in configuration.Controllers)
            {
                if (controller.Columns == null)
                    continue;

                foreach (var column in controller.Columns)
                {
                    column.Controller = controller;
                }
            }

            return configuration.Controllers;
        }

        public async Task<ObservableCollection<CashRegister>> GetCashRegistersAsync()
        {
            var configuration = await ReadConfigurationFileAsync();

            return configuration.CashRegisters;
        }

        /// <inheritdoc/>
        public async Task<ObservableCollection<Column>> GetColumnsAsync()
        {
            var configuration = await ReadConfigurationFileAsync();

            var columns = configuration.Controllers
                .SelectMany(controller => controller.Columns.Select(column =>
                {
                    column.Controller = controller;
                    return column;
                }))
                .ToList();

            return new(columns);
        }

        /// <inheritdoc/>
        public async Task<ObservableCollection<Column>> GetColumnsByControllerIdAndAddressAsync(Guid controllerId, int address)
        {
            var configuration = await ReadConfigurationFileAsync();

            var filteredColumns = configuration.Controllers
                .Where(c => c.Id == controllerId)
                // Перебираем все контроллеры
                .SelectMany(controller =>
                    // Берём только те колонки, у которых Address == нужному значению
                    controller.Columns
                              .Where(column => column.Address == address)
                              // Дополнительно «проставляем» ссылку на Controller
                              .Select(column =>
                              {
                                  column.Controller = controller;
                                  return column;
                              })
                )
                .ToList(); // получаем обычный список

            // Превращаем в ObservableCollection и возвращаем
            return new ObservableCollection<Column>(filteredColumns);
        }

        /// <inheritdoc/>
        public async Task<Column?> GetColumnByIdAsync(Guid id)
        {
            var configuration = await ReadConfigurationFileAsync();

            var column = configuration.Controllers
                .SelectMany(controller => controller.Columns.Select(column =>
                {
                    column.Controller = controller; // Устанавливаем связь с Controller
                    return column;
                }))
                .FirstOrDefault(column => column.Id == id); // Поиск по ID

            return column;
        }

        /// <inheritdoc/>
        public async Task<CashRegister?> GetCashRegisterAsync(Guid id)
        {
            var configuration = await ReadConfigurationFileAsync();

            var cashRegister = configuration.CashRegisters
                .FirstOrDefault(cashRegister => cashRegister.Id == id); // Поиск по ID

            return cashRegister;
        }

        /// <inheritdoc/>
        public async Task SaveControllerAsync(Controller controller)
        {
            try
            {
                var configuration = await ReadConfigurationFileAsync();
                var existingController = configuration.Controllers.FirstOrDefault(c => c.Id == controller.Id);

                if (existingController != null)
                {
                    // Обновление существующего контроллера
                    existingController.Update(controller);
                }
                else
                {
                    // Добавление нового контроллера
                    configuration.Controllers.Add(controller);
                }

                await SaveConfigurationFileAsync(configuration);

                OnControllerPropertyChanged?.Invoke(controller);
            }
            catch (Exception)
            {

            }
        }

        public async Task SaveColumnAsync(Column column)
        {
            try
            {

            }
            catch (Exception)
            {

            }
        }

        public async Task SaveCashRegisterAsync(CashRegister cashRegister)
        {
            try
            {
                var configuration = await ReadConfigurationFileAsync();
                var existingCashRegister = configuration.CashRegisters.FirstOrDefault(c => c.Id == cashRegister.Id);

                if (existingCashRegister != null)
                {
                    // Обновление существующего контроллера
                    existingCashRegister.Update(cashRegister);
                }
                else
                {
                    // Добавление нового контроллера
                    configuration.CashRegisters.Add(cashRegister);
                }

                await SaveConfigurationFileAsync(configuration);

                OnCashRegisterPropertyChanged?.Invoke(cashRegister);
            }
            catch (Exception)
            {

            }
        }

        /// <inheritdoc/>
        public async Task<bool> RemoveControllerAsync(Guid controllerId)
        {
            try
            {
                var configuration = await ReadConfigurationFileAsync();
                var controller = configuration.Controllers.FirstOrDefault(c => c.Id == controllerId);

                if (controller != null)
                {
                    configuration.Controllers.Remove(controller);
                    await SaveConfigurationFileAsync(configuration);
                    return true;
                }
                else
                {
                    return false;
                    throw new InvalidOperationException("Контроллер с указанным ID не найден.");
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> RemoveCashRegisterAsync(Guid cashRegisterId)
        {
            try
            {
                var configuration = await ReadConfigurationFileAsync();
                var cashRegister = configuration.CashRegisters.FirstOrDefault(c => c.Id == cashRegisterId);

                if (cashRegister != null)
                {
                    configuration.CashRegisters.Remove(cashRegister);
                    await SaveConfigurationFileAsync(configuration);
                    return true;
                }
                else
                {
                    return false;
                    throw new InvalidOperationException("ККМ с указанным ID не найден.");
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion
    }
}
