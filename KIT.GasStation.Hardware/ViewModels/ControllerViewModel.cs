using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using KIT.GasStation.Hardware.Utilities;
using KIT.GasStation.HardwareConfigurations.Models;
using KIT.GasStation.HardwareConfigurations.Services;
using System.Collections.Generic;

namespace KIT.GasStation.Hardware.ViewModels
{
    public class ControllerViewModel : BaseViewModel
    {
        #region Private Members

        private Controller _createdController = new();
        private readonly IHardwareConfigurationService _hardwareConfigurationService;

        #endregion

        #region Public Properties

        /// <summary>
        /// Созданный контроллер
        /// </summary>
        public Controller CreatedController
        {
            get => _createdController;
            set
            {
                _createdController = value;
                OnPropertyChanged(nameof(CreatedController));
            }
        }

        /// <summary>
        /// Список типов контроллеров
        /// </summary>
        public List<KeyValuePair<ControllerType, string>> ControllerTypes => new(EnumHelper.GetLocalizedEnumValues<ControllerType>());

        #endregion

        #region Constructors

        public ControllerViewModel(IHardwareConfigurationService hardwareConfigurationService)
        {
            _hardwareConfigurationService = hardwareConfigurationService;
        }

        #endregion

        #region Commands

        [Command]
        public void Save()
        {
            if (CheckController())
            {
                _hardwareConfigurationService.SaveControllerAsync(CreatedController);
                CurrentWindowService?.Close();
            }
        }

        [Command]
        public void Close()
        {
            CurrentWindowService?.Close();
        }

        #endregion

        #region Private Methods

        private bool CheckController()
        {
            if (CreatedController.Type == ControllerType.None)
            {
                MessageBoxService.ShowMessage("Выберите тип контроллера", "Ошибка", MessageButton.OK, MessageIcon.Error);
                return false;
            }

            if (string.IsNullOrWhiteSpace(CreatedController.Name))
            {
                MessageBoxService.ShowMessage("Введите имя контроллера", "Ошибка", MessageButton.OK, MessageIcon.Error);
                return false;
            }

            return true;
        }

        #endregion
    }
}
