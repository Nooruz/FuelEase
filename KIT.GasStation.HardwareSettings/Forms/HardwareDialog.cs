using KIT.GasStation.HardwareConfigurations.Models;
using KIT.GasStation.HardwareSettings.Models;
using KIT.GasStation.HardwareSettings.Views;

namespace KIT.GasStation.HardwareSettings.Forms
{
    public partial class HardwareDialog : Form, IHardwareDialog
    {
        #region Private Members

        private object? _model;

        #endregion

        #region Events

        public event EventHandler? CreateDeviceClicked;

        #endregion

        #region Constructors

        public HardwareDialog(HardwareModel<Controller, ControllerType> model)
        {
            InitializeComponent();
            AttachPresenter(model);
        }

        public HardwareDialog(HardwareModel<CashRegister, CashRegisterType> model)
        {
            InitializeComponent();
            AttachPresenter(model);
        }

        #endregion

        #region Public Voids

        public void AttachPresenter(HardwareModel<Controller, ControllerType> model)
        {
            _model = model;
            cmbDeviceType.DisplayMember = "Value";
            cmbDeviceType.ValueMember = "Key";
            cmbDeviceType.DataSource = model.DeviceTypes;

            Text = "Создание ТРК";
        }

        public void AttachPresenter(HardwareModel<CashRegister, CashRegisterType> model)
        {
            _model = model;
            cmbDeviceType.DisplayMember = "Value";
            cmbDeviceType.ValueMember = "Key";
            cmbDeviceType.DataSource = model.DeviceTypes;

            Text = "Создание кассы";
        }

        #endregion

        #region Event Handlers

        private void btnOk_Click(object sender, EventArgs e)
        {
            if (_model is HardwareModel<Controller, ControllerType> controllerModel)
            {
                if (cmbDeviceType.SelectedValue is ControllerType type)
                    controllerModel.CreatedDevice.Type = type;
                controllerModel.CreatedDevice.Name = tbDeviceName.Text;

                if (!controllerModel.CheckDevice())
                    return;

                controllerModel.SaveDeviceAsync();
                DialogResult = DialogResult.OK;
                Close();
            }
            else if (_model is HardwareModel<CashRegister, CashRegisterType> cashRegisterModel)
            {
                if (cmbDeviceType.SelectedValue is CashRegisterType type)
                    cashRegisterModel.CreatedDevice.Type = type;
                cashRegisterModel.CreatedDevice.Name = tbDeviceName.Text;

                if (!cashRegisterModel.CheckDevice())
                    return;

                cashRegisterModel.SaveDeviceAsync();
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        #endregion
    }
}
