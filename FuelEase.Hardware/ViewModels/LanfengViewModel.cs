using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using FuelEase.Common.Factories;
using FuelEase.FuelDispenser.Services;
using FuelEase.Hardware.Utilities;
using FuelEase.Hardware.ViewModels.Base;
using FuelEase.Hardware.Views;
using FuelEase.HardwareConfigurations.Models;
using FuelEase.HardwareConfigurations.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FuelEase.Hardware.ViewModels
{
    public class LanfengViewModel : BaseFuelDispenserModel
    {
        #region Private Members

        private readonly IHardwareConfigurationService _hardwareConfigurationService;
        private readonly IFuelDispenserFactory _fuelDispenserFactory;

        #endregion

        #region Public Voids

        public List<KeyValuePair<LanfengFuelRequestType, string>> LanfengFuelRequestTypes => new(EnumHelper.GetLocalizedEnumValues<LanfengFuelRequestType>());

        #endregion

        #region Constructors

        public LanfengViewModel(IHardwareConfigurationService hardwareConfigurationService,
            IFuelDispenserFactory fuelDispenserFactory)
        {
            _hardwareConfigurationService = hardwareConfigurationService;
            _fuelDispenserFactory = fuelDispenserFactory;
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
                    Settings = new LanfengColumnSettings()
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
            await _hardwareConfigurationService.SaveControllerAsync(SelectedController);
        }

        [Command]
        public void Settings()
        {
            try
            {
                WindowService.Title = "Настройки контроллера";
                WindowService.Show(nameof(LanfengSettingsView), new LanfengSettingsViewModel());
            }
            catch (Exception)
            {
                //ignore
            }
        }

        [Command (CanExecuteMethodName = nameof(CanCheckStatus))]
        public async Task CheckStatus()
        {
            // Блокируем повторное нажатие
            AllowCheckStatus = false;

            // Устанавливаем статус проверки
            SelectedColumn.ConnectionStatus = ConnectionStatus.BeingVerified;

            try
            {
                // Создаём сервис для работы с колонкой
                IFuelDispenserService fuelDispenserService = _fuelDispenserFactory.Create(ControllerType.Lanfeng);

                // Подключаемся к колонке
                //await fuelDispenserService.Connect(SelectedController.ComPort, SelectedController.BaudRate);

                //await fuelDispenserService.InitializeAsync(0); 

                // Получаем версию и имя
                var version = fuelDispenserService.Version;
                var name = fuelDispenserService.DispenserName;

                // Проверяем статус
                //var status = await fuelDispenserService.CheckStatusAsync(SelectedColumn);

                //Если статус не None, то устанавливаем статус Connected, иначе NotConnected
                //if (status != ColumnStatus.None)
                //{
                //    SelectedColumn.ConnectionStatus = ConnectionStatus.Connected;
                //}
                //else
                //{
                //    SelectedColumn.ConnectionStatus = ConnectionStatus.NotConnected;
                //}
            }
            catch (Exception)
            {
                SelectedColumn.ConnectionStatus = ConnectionStatus.NotConnected;
            }
            finally
            {
                AllowCheckStatus = true;
            }
        }

        #endregion
    }
}
