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
        public event EventHandler? AddCashRegisterClicked;
        public event EventHandler<PageType>? NavigateRequested;

        #endregion

        #region Public Properties

        public Control ContentHost => panelContent;
        public TreeView TreeViewControl => treeView1;
        public TreeNode ControllersNode => nodeControllers;
        public TreeNode CashRegistersNode => nodeCashRegisters;
        public ContextMenuStrip ControllerItemContextMenu => cmsControllerItem;
        public ContextMenuStrip CashRegisterItemContextMenu => cmsCashRegisterItem;

        #endregion

        #region Constructors

        public Main()
        {
            InitializeComponent();
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

        #region Event Handlers

        private void tsmiAddFuelDispenser_Click(object sender, EventArgs e)
            => AddFuelDispenserClicked?.Invoke(this, e);

        private void tsmiAddCashRegister_Click(object sender, EventArgs e)
            => AddCashRegisterClicked?.Invoke(this, e);

        private void tsmiDeleteController_Click(object sender, EventArgs e)
        {
            _presenter?.DeleteSelectedController();
        }

        private void tsmiDeleteCashRegister_Click(object sender, EventArgs e)
        {
            _presenter?.DeleteSelectedCashRegister();
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            _presenter?.OnTreeViewNodeSelected(e.Node);
        }

        private void treeView1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var hitTest = treeView1.HitTest(e.Location);
                if (hitTest.Node != null)
                {
                    treeView1.SelectedNode = hitTest.Node;
                }
            }
        }

        #endregion
    }
}
