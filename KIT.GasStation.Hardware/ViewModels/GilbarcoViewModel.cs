using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using KIT.GasStation.Common.Factories;
using KIT.GasStation.Hardware.Utilities;
using KIT.GasStation.Hardware.ViewModels.Base;
using KIT.GasStation.Hardware.Views;
using KIT.GasStation.HardwareConfigurations.Models;
using KIT.GasStation.HardwareConfigurations.Services;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading.Tasks;

namespace KIT.GasStation.Hardware.ViewModels
{
    public class GilbarcoViewModel : BaseFuelDispenserModel
    {
        #region Private Members

        private readonly IHardwareConfigurationService _hardwareConfigurationService;
        private ColumnQuantity _selectedColumnQuantity = ColumnQuantity.Three;

        #endregion

        #region Public Properties

        public List<Parity> Parities => new() { Parity.Even, Parity.Odd, Parity.None };
        public List<KeyValuePair<ColumnQuantity, string>> ColumnQuantities => new(EnumHelper.GetLocalizedEnumValues<ColumnQuantity>());
        public List<KeyValuePair<PriceDecimalPoint, string>> PriceDecimalPoints => new(EnumHelper.GetLocalizedEnumValues<PriceDecimalPoint>());
        public ColumnQuantity SelectedColumnQuantity
        {
            get => _selectedColumnQuantity;
            set
            {
                _selectedColumnQuantity = value;
                OnPropertyChanged(nameof(SelectedColumnQuantity));
            }
        }

        #endregion

        #region Constructors

        public GilbarcoViewModel(IHardwareConfigurationService hardwareConfigurationService)
        {
            _hardwareConfigurationService = hardwareConfigurationService;

            BaudRates = new() { 4800, 5787, 9600 };
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
                    Settings = new GilbarcoColumnSettings()
                    {
                        ColumnQuantity = SelectedColumnQuantity,
                        PriceDecimalPoint = PriceDecimalPoint.Two
                    }
                };

                SelectedController.Columns.Add(newColumn);
            }

        }

        [Command]
        public async Task Save()
        {
            await _hardwareConfigurationService.SaveControllerAsync(SelectedController);
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

        #endregion
    }
}
