using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using KIT.GasStation.Hardware.Services;
using KIT.GasStation.Hardware.Utilities;
using KIT.GasStation.HardwareConfigurations.Services;
using System;
using System.Collections.Generic;

namespace KIT.GasStation.Hardware.ViewModels
{
    public class DeviceViewModel<TDevice, TType> : BaseViewModel
    where TDevice : IDevice<TType>, new()
    where TType : Enum
    {
        #region Private Members

        private TDevice _createdDevice = new();
        private readonly IDeviceService<TDevice> _deviceService;

        #endregion

        #region Public Properties

        public TDevice CreatedDevice
        {
            get => _createdDevice;
            set
            {
                _createdDevice = value;
                OnPropertyChanged(nameof(CreatedDevice));
            }
        }

        public List<KeyValuePair<TType, string>> DeviceTypes =>
            new(EnumHelper.GetLocalizedEnumValues<TType>());

        #endregion

        #region Constructors

        public DeviceViewModel(IDeviceService<TDevice> deviceService)
        {
            _deviceService = deviceService;
        }

        #endregion

        #region Commands

        [Command]
        public void Save()
        {
            if (CheckDevice())
            {
                _deviceService.SaveDeviceAsync(CreatedDevice);
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

        private bool CheckDevice()
        {
            if (EqualityComparer<TType>.Default.Equals(CreatedDevice.Type, default))
            {
                MessageBoxService.ShowMessage("Выберите тип устройства", "Ошибка",
                    MessageButton.OK, MessageIcon.Error);
                return false;
            }

            if (string.IsNullOrWhiteSpace(CreatedDevice.Name))
            {
                MessageBoxService.ShowMessage("Введите имя устройства", "Ошибка",
                    MessageButton.OK, MessageIcon.Error);
                return false;
            }

            return true;
        }

        #endregion
    }
}
