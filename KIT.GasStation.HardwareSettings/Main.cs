using KIT.GasStation.HardwareSettings.CustomControl;
using KIT.GasStation.HardwareSettings.Presenters;
using KIT.GasStation.HardwareSettings.Views;

namespace KIT.GasStation.HardwareSettings
{
    public partial class Main : Form, IMainView
    {
        #region Private Members

        private MainPresenter? _presenter;

        #endregion

        #region Events

        public event EventHandler? AddFuelDispenserClicked;
        public event EventHandler<PageType>? NavigateRequested;

        #endregion

        #region Public Properties

        public Control ContentHost => panelContent;

        #endregion

        #region Constructors

        public Main()
        {
            InitializeComponent();

            // опционально: double buffer, чтоб не мигало
            this.DoubleBuffered = true;
        }

        #endregion

        #region Public Voids

        public void AttachPresenter(MainPresenter presenter)
        => _presenter = presenter;

        public void ShowContent(Control content)
        {
            content.Dock = DockStyle.Fill;

            panelContent.SuspendLayout();
            panelContent.Controls.Clear();
            panelContent.Controls.Add(content);
            panelContent.ResumeLayout();
        }

        #endregion

        private void tsmiAddFuelDispenser_Click(object sender, EventArgs e)
            => AddFuelDispenserClicked?.Invoke(this, e);
    }
}
