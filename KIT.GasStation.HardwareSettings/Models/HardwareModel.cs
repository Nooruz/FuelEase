using KIT.GasStation.HardwareConfigurations.Services;
using KIT.GasStation.HardwareSettings.Helpers;
using KIT.GasStation.HardwareSettings.Services;

namespace KIT.GasStation.HardwareSettings.Models
{
    public sealed class HardwareModel<TDevice, TType> : BaseModel
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

        public HardwareModel(IDeviceService<TDevice> deviceService)
        {
            _deviceService = deviceService;
        }

        #endregion

        #region Public Methods

        public bool CheckDevice()
        {
            if (EqualityComparer<TType>.Default.Equals(CreatedDevice.Type, default))
            {
                MessageBox.Show("Выберите тип устройства", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (string.IsNullOrWhiteSpace(CreatedDevice.Name))
            {
                MessageBox.Show("Введите имя устройства", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            return true;
        }

        public async void SaveDeviceAsync()
        {
            await _deviceService.SaveDeviceAsync(CreatedDevice);
        }

        #endregion
    }

    public enum Hardware
    {
        Controller,
        CashRegister
    }
}
