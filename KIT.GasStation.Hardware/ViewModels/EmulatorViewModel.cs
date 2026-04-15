using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using KIT.GasStation.Hardware.ViewModels.Base;
using KIT.GasStation.Hardware.Views;
using KIT.GasStation.HardwareConfigurations.Models;
using KIT.GasStation.HardwareConfigurations.Services;
using System.Threading.Tasks;

namespace KIT.GasStation.Hardware.ViewModels
{
    public class EmulatorViewModel : BaseFuelDispenserModel
    {
        #region Private Members

        private readonly IHardwareConfigurationService _hardwareConfigurationService;

        #endregion

        #region Constructors

        public EmulatorViewModel(IHardwareConfigurationService hardwareConfigurationService)
        {
            _hardwareConfigurationService = hardwareConfigurationService;
        }

        #endregion

        #region Commands

        [Command]
        public void AddColumn()
        {
            var viewModel = new ColumnCountViewModel();
            WindowService.Title = "Добавление колонок";
            WindowService.Show(nameof(ColumnCountView), viewModel);

            // Если пользователь не подтвердил добавление колонок, выходим
            if (viewModel.Count <= 0) return;

            // Сколько новых колонок нужно добавить
            int newColumnsCount = viewModel.Count;

            // Сколько колонок у нас уже есть
            int currentCount = SelectedController.Columns.Count;

            // Пример логики заполнения:
            //   - Address будем считать циклически 0..3
            //   - Nozzle — 1..4
            //   - Name — «Колонка_{номер}»

            // Добавляем новые колонки (строки)
            for (int i = 0; i < newColumnsCount; i++)
            {
                // Общий индекс = уже имеющиеся + i-я по счёту новая
                int totalIndex = currentCount + i;

                // Address вычисляем, как целую часть деления на 4
                // Это даёт группы по 4 строки с одинаковым Address.
                int address = totalIndex / 4;

                // Pistol циклически идёт от 1 до 4
                int pistol = (totalIndex % 4) + 1;

                // Пример формирования имени
                string name = $"Колонка_{SelectedController.Columns.Count + 1}";

                // Создаём новый объект и добавляем в коллекцию
                var newColumn = new Column
                {
                    Address = address,
                    Nozzle = pistol,
                    Name = name,
                    Settings = new EmulatorColumnSettings()
                };

                SelectedController.Columns.Add(newColumn);
            }

        }

        [Command]
        public void DeleteColumn()
        {
            if (SelectedColumn != null)
            {
                var result = MessageBoxService.ShowMessage("Удалить колонку?", "Подтверждение", MessageButton.YesNo, MessageIcon.Question);
                if (result == MessageResult.No) return;
                SelectedController.Columns.Remove(SelectedColumn);
            }
        }

        [Command]
        public async Task Save()
        {
            if (SelectedController == null) return;

            if (SelectedController.Columns.Count == 0) return;

            foreach (var item in SelectedController.Columns)
            {
                if (item.SystemCounter < 0)
                {
                    MessageBoxService.ShowMessage($"Системный счётчик колонки \"{item.Name}\" не может быть отрицательным.", "Ошибка", MessageButton.OK, MessageIcon.Error);
                    return;
                }
            }

            await _hardwareConfigurationService.SaveControllerAsync(SelectedController);
        }

        [Command(CanExecuteMethodName = nameof(CanCheckStatus))]
        public async Task CheckStatus()
        {
            // Блокируем повторное нажатие
            AllowCheckStatus = false;

            // Устанавливаем статус проверки
            SelectedColumn.ConnectionStatus = ConnectionStatus.BeingVerified;

            await Task.Delay(1000);

            SelectedColumn.ConnectionStatus = ConnectionStatus.Connected;

            await Task.Delay(1000);

            AllowCheckStatus = true;
        }

        #endregion
    }
}
