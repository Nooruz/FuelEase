namespace KIT.GasStation.HardwareSettings.Forms
{
    public partial class ColumnCountDialog : Form
    {
        #region Public Properties

        public int ColumnCount { get; private set; }

        #endregion

        #region Constructors

        public ColumnCountDialog()
        {
            InitializeComponent();
            numCount.Minimum = 1;
            numCount.Maximum = 100;
            numCount.Value = 1;
        }

        #endregion

        #region Event Handlers

        private void btnOk_Click(object sender, EventArgs e)
        {
            ColumnCount = (int)numCount.Value;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            ColumnCount = 0;
            DialogResult = DialogResult.Cancel;
            Close();
        }

        #endregion
    }
}
