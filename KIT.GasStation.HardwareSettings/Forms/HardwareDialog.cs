using KIT.GasStation.HardwareConfigurations.Models;
using KIT.GasStation.HardwareSettings.Models;
using KIT.GasStation.HardwareSettings.Presenters;
using KIT.GasStation.HardwareSettings.Views;

namespace KIT.GasStation.HardwareSettings.Forms
{
    public partial class HardwareDialog : Form, IHardwareDialog
    {
        #region Events

        public event EventHandler? CreateDeviceClicked;

        #endregion

        #region Constructors

        public HardwareDialog(HardwareModel<Controller, ControllerType> model)
        {
            InitializeComponent();

            
        }

        #endregion

        #region Public Voids

        public void AttachPresenter(HardwareModel<Controller, ControllerType> model)
        {
            cmbDeviceType.DisplayMember = "Value";
            cmbDeviceType.ValueMember = "Key";
            cmbDeviceType.DataSource = model.DeviceTypes;
        }

        #endregion

        private void btnOk_Click(object sender, EventArgs e)
        {

        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
