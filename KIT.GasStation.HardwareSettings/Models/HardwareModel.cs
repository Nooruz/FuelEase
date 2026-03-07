using KIT.App.Infrastructure.Helpers;
using KIT.App.Infrastructure.Services;
using KIT.GasStation.HardwareConfigurations.Services;
using KIT.GasStation.HardwareSettings.Views;

namespace KIT.GasStation.HardwareSettings.Models
{
    public sealed class HardwareModel<TDevice, TType> : BaseModel
        where TDevice : IDevice<TType>, new()
        where TType : Enum
    {
        #region Private Members

        private TDevice _createdDevice = new();
        private readonly IDeviceService<TDevice> _deviceService;
        private readonly IHardwareDialog _hardwareDialog;

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

        public HardwareModel(IDeviceService<TDevice> deviceService,
            IHardwareDialog hardwareDialog)
        {
            _deviceService = deviceService;
            _hardwareDialog = hardwareDialog;

            _hardwareDialog.AttachPresenter(this);
        }

        #endregion

        #region Private Methods

        private bool CheckDevice()
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

        #endregion
    }

    public enum Hardware
    {
        Controller,
        CashRegister
    }
}
