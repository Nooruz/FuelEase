using KIT.App.Infrastructure.Helpers;
using KIT.App.Infrastructure.Services;
using KIT.GasStation.HardwareConfigurations.Models;
using KIT.GasStation.HardwareConfigurations.Models.ColumnSettings;
using KIT.GasStation.HardwareConfigurations.Models.ColumnSettings.ColumnSettings;
using KIT.GasStation.HardwareConfigurations.Models.ColumnSettings.ColumnSettings.ColumnSettings;
using KIT.GasStation.HardwareConfigurations.Models.ColumnSettings.ColumnSettings.ColumnSettings.ColumnSettings;
using KIT.GasStation.HardwareConfigurations.Models.ColumnSettings.ColumnSettings.ColumnSettings.ColumnSettings.ColumnSettings;
using KIT.GasStation.HardwareConfigurations.Models.ColumnSettings.ColumnSettings.ColumnSettings.ColumnSettings.ColumnSettings.ColumnSettings;
using KIT.GasStation.HardwareConfigurations.Models.ColumnSettings.ColumnSettings.ColumnSettings.ColumnSettings.ColumnSettings.ColumnSettings.ColumnSettings;
using KIT.GasStation.HardwareConfigurations.Models.ColumnSettings.ColumnSettings.ColumnSettings.ColumnSettings.ColumnSettings.ColumnSettings.ColumnSettings.ColumnSettings;
using KIT.GasStation.HardwareConfigurations.Models.ColumnSettings.ColumnSettings.ColumnSettings.ColumnSettings.ColumnSettings.ColumnSettings.ColumnSettings.ColumnSettings.ColumnSettings;
using KIT.GasStation.HardwareConfigurations.Services;
using KIT.GasStation.HardwareSettings.CustomControl;
using KIT.GasStation.HardwareSettings.CustomControl.Factories;
using KIT.GasStation.HardwareSettings.CustomControl.Views;
using KIT.GasStation.HardwareSettings.Forms;
using KIT.GasStation.HardwareSettings.Models;
using KIT.GasStation.HardwareSettings.Services;
using KIT.GasStation.HardwareSettings.Views;
using System.Collections.ObjectModel;
using System.IO.Ports;

namespace KIT.GasStation.HardwareSettings.Presenters
{
    public class MainPresenter
    {
        #region Private Members

        private readonly IMainView _view;
        private readonly IPageFactory _pages;
        private readonly IDialogService _dialogService;
        private readonly IHardwareConfigurationService _hardwareConfigurationService;
        private IPage? _current;

        private ObservableCollection<Controller> _controllers = new();
        private ObservableCollection<CashRegister> _cashRegisters = new();

        #endregion

        #region Constructors

        public MainPresenter(IMainView view,
            IPageFactory pages,
            IDialogService dialogService,
            IHardwareConfigurationService hardwareConfigurationService)
        {
            _view = view;
            _pages = pages;
            _dialogService = dialogService;
            _hardwareConfigurationService = hardwareConfigurationService;

            _view.AttachPresenter(this);

            _view.AddFuelDispenserClicked += View_AddFuelDispenserClicked;
            _view.AddCashRegisterClicked += View_AddCashRegisterClicked;

            _hardwareConfigurationService.OnControllerPropertyChanged += OnControllerPropertyChanged;
            _hardwareConfigurationService.OnCashRegisterPropertyChanged += OnCashRegisterPropertyChanged;

            InitializeConfigurationAsync();
        }

        #endregion

        #region Public Methods

        public void OnTreeViewNodeSelected(TreeNode? node)
        {
            if (node == null) return;

            // Если выбран дочерний узел контроллера
            if (node.Tag is Controller controller)
            {
                NavigateToController(controller);
                return;
            }

            // Если выбран дочерний узел кассы
            if (node.Tag is CashRegister cashRegister)
            {
                NavigateToCashRegister(cashRegister);
                return;
            }
        }

        public async void DeleteSelectedController()
        {
            var selectedNode = _view.TreeViewControl.SelectedNode;
            if (selectedNode?.Tag is not Controller controller) return;

            var result = MessageBox.Show(
                $"Удалить выбранный ТРК? \"{controller.Name}\"",
                "Подтверждение",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                if (await _hardwareConfigurationService.RemoveControllerAsync(controller.Id))
                {
                    _controllers.Remove(controller);
                    selectedNode.Remove();
                    ClearContent();
                }
                else
                {
                    MessageBox.Show("Ошибка удаления ТРК", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        public async void DeleteSelectedCashRegister()
        {
            var selectedNode = _view.TreeViewControl.SelectedNode;
            if (selectedNode?.Tag is not CashRegister cashRegister) return;

            var result = MessageBox.Show(
                $"Удалить выбранную кассу? \"{cashRegister.Name}\"",
                "Подтверждение",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                if (await _hardwareConfigurationService.RemoveCashRegisterAsync(cashRegister.Id))
                {
                    _cashRegisters.Remove(cashRegister);
                    selectedNode.Remove();
                    ClearContent();
                }
                else
                {
                    MessageBox.Show("Ошибка удаления кассы", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        #endregion

        #region Private Methods — Initialization

        private async void InitializeConfigurationAsync()
        {
            try
            {
                await _hardwareConfigurationService.EnsureConfigurationFileExistsAsync();

                _controllers = await _hardwareConfigurationService.GetControllersAsync();
                _cashRegisters = await _hardwareConfigurationService.GetCashRegistersAsync();

                PopulateTreeView();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации конфигурации: {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PopulateTreeView()
        {
            _view.ControllersNode.Nodes.Clear();
            foreach (var controller in _controllers)
            {
                AddControllerToTree(controller);
            }

            _view.CashRegistersNode.Nodes.Clear();
            foreach (var cashRegister in _cashRegisters)
            {
                AddCashRegisterToTree(cashRegister);
            }

            _view.ControllersNode.Expand();
            _view.CashRegistersNode.Expand();
        }

        #endregion

        #region Private Methods — Tree

        private TreeNode AddControllerToTree(Controller controller)
        {
            var node = new TreeNode(controller.Name)
            {
                Tag = controller,
                ContextMenuStrip = GetControllerItemContextMenu()
            };
            _view.ControllersNode.Nodes.Add(node);
            return node;
        }

        private TreeNode AddCashRegisterToTree(CashRegister cashRegister)
        {
            var node = new TreeNode(cashRegister.Name)
            {
                Tag = cashRegister,
                ContextMenuStrip = GetCashRegisterItemContextMenu()
            };
            _view.CashRegistersNode.Nodes.Add(node);
            return node;
        }

        private ContextMenuStrip GetControllerItemContextMenu()
        {
            // Reuse the cmsControllerItem from the Main form
            if (_view is Main mainForm)
            {
                return mainForm.Controls.Find("", true).Length > 0
                    ? new ContextMenuStrip() : new ContextMenuStrip();
            }
            var cms = new ContextMenuStrip();
            var deleteItem = new ToolStripMenuItem("Удалить");
            deleteItem.Click += (s, e) => DeleteSelectedController();
            cms.Items.Add(deleteItem);
            return cms;
        }

        private ContextMenuStrip GetCashRegisterItemContextMenu()
        {
            var cms = new ContextMenuStrip();
            var deleteItem = new ToolStripMenuItem("Удалить");
            deleteItem.Click += (s, e) => DeleteSelectedCashRegister();
            cms.Items.Add(deleteItem);
            return cms;
        }

        #endregion

        #region Private Methods — Navigation

        private void NavigateToController(Controller controller)
        {
            _current?.Dispose();
            _current = null;

            switch (controller.Type)
            {
                case ControllerType.Lanfeng:
                    NavigateToLanfeng(controller);
                    break;
                case ControllerType.Gilbarco:
                    NavigateToGilbarco(controller);
                    break;
                case ControllerType.Emulator:
                    NavigateToEmulator(controller);
                    break;
                case ControllerType.PKElectronics:
                    NavigateToPKElectronics(controller);
                    break;
                default:
                    ClearContent();
                    break;
            }
        }

        private void NavigateToCashRegister(CashRegister cashRegister)
        {
            _current?.Dispose();
            _current = null;

            switch (cashRegister.Type)
            {
                case CashRegisterType.EKassa:
                    NavigateToEKassa(cashRegister);
                    break;
                case CashRegisterType.NewCas:
                    NavigateToNewCas(cashRegister);
                    break;
                default:
                    ClearContent();
                    break;
            }
        }

        #endregion

        #region Private Methods — Lanfeng

        private void NavigateToLanfeng(Controller controller)
        {
            _current = _pages.Create(PageType.Lanfeng);
            var view = (LanfengView)_current.View;

            // Заполняем порты и скорости
            view.cmbPort.Items.Clear();
            foreach (var port in SerialPort.GetPortNames())
                view.cmbPort.Items.Add(port);
            if (!string.IsNullOrEmpty(controller.ComPort))
                view.cmbPort.SelectedItem = controller.ComPort;

            var baudRates = new int[] { 2400, 4800, 9550, 9600, 9650, 9700, 9750, 10500, 10600, 57600, 115200 };
            view.cmbBaudrate.Items.Clear();
            foreach (var rate in baudRates)
                view.cmbBaudrate.Items.Add(rate);
            view.cmbBaudrate.SelectedItem = controller.BaudRate;

            // Заполняем таблицу колонок
            SetupFuelDispenserGrid(view.dgvColumns, controller, true);

            // Кнопки
            view.btnAddColumn.Click += (s, e) => AddColumnsToController(controller, view.dgvColumns,
                () => new LanfengColumnSettings());
            view.btnDeleteColumn.Click += (s, e) => DeleteSelectedColumn(controller, view.dgvColumns);
            view.btnSave.Click += async (s, e) =>
            {
                ReadPortAndBaudRate(view.cmbPort, view.cmbBaudrate, controller);
                await _hardwareConfigurationService.SaveControllerAsync(controller);
            };

            _current.OnShow();
            _view.ShowContent(_current.View);
        }

        #endregion

        #region Private Methods — PKElectronics

        private void NavigateToPKElectronics(Controller controller)
        {
            _current = _pages.Create(PageType.PKElectronics);
            var view = (PKElectronicsView)_current.View;

            // Порты и скорости
            FillPortsCombo(view.cmbPort, controller.ComPort);
            var baudRates = new int[] { 2400, 4800, 9550, 9600, 9650, 9700, 9750, 10500, 10600, 57600, 115200 };
            FillCombo(view.cmbBaudRate, baudRates, controller.BaudRate);

            // Метод опроса
            var pollingModes = EnumHelper.GetLocalizedEnumValues<PollingMode>();
            view.cmbPollingMode.DisplayMember = "Value";
            view.cmbPollingMode.ValueMember = "Key";
            view.cmbPollingMode.DataSource = pollingModes;
            if (controller.Settings is PKElectronicsControllerSettings pkSettings)
                view.cmbPollingMode.SelectedValue = pkSettings.PollingMode;

            // Количество пистолетов на стороне
            var nozzlesPerSides = EnumHelper.GetLocalizedEnumValues<NozzlesPerSide>();
            view.cmbNozzlesPerSide.DisplayMember = "Value";
            view.cmbNozzlesPerSide.ValueMember = "Key";
            view.cmbNozzlesPerSide.DataSource = nozzlesPerSides;
            if (controller.Settings is PKElectronicsControllerSettings pkSettings2)
                view.cmbNozzlesPerSide.SelectedValue = pkSettings2.NozzlesPerSide;

            // Таблица колонок
            SetupFuelDispenserGrid(view.dgvColumns, controller, false);

            // Кнопки
            view.btnAddColumn.Click += (s, e) => AddColumnsToController(controller, view.dgvColumns,
                () => new PKElectronicsColumnSettings());
            view.btnDeleteColumn.Click += (s, e) => DeleteSelectedColumn(controller, view.dgvColumns);
            view.btnSave.Click += async (s, e) =>
            {
                ReadPortAndBaudRate(view.cmbPort, view.cmbBaudRate, controller);
                await _hardwareConfigurationService.SaveControllerAsync(controller);
            };

            _current.OnShow();
            _view.ShowContent(_current.View);
        }

        #endregion

        #region Private Methods — Gilbarco

        private void NavigateToGilbarco(Controller controller)
        {
            _current = _pages.Create(PageType.Gilbarco);
            var view = (GilbarcoView)_current.View;

            // Порты и скорости
            FillPortsCombo(view.cmbPort, controller.ComPort);
            var baudRates = new int[] { 4800, 5787, 9600 };
            FillCombo(view.cmbBaudRate, baudRates, controller.BaudRate);

            // Контроль (Parity)
            view.cmbParity.Items.Clear();
            view.cmbParity.Items.Add(Parity.Even);
            view.cmbParity.Items.Add(Parity.Odd);
            view.cmbParity.Items.Add(Parity.None);
            if (controller.Settings is GilbarcoControllerSettings gilSettings)
            {
                view.cmbParity.SelectedItem = gilSettings.Parity;
                view.chkEchoSuppression.Checked = gilSettings.EchoSuppression;
            }

            // Количество пистолетов на стороне
            var columnQuantities = EnumHelper.GetLocalizedEnumValues<ColumnQuantity>();
            view.cmbColumnQuantity.DisplayMember = "Value";
            view.cmbColumnQuantity.ValueMember = "Key";
            view.cmbColumnQuantity.DataSource = columnQuantities;

            // Таблица колонок
            SetupFuelDispenserGrid(view.dgvColumns, controller, false);

            // Кнопки
            view.btnAddColumn.Click += (s, e) =>
            {
                var selectedQuantity = view.cmbColumnQuantity.SelectedValue is ColumnQuantity cq
                    ? cq : ColumnQuantity.Three;
                AddColumnsToController(controller, view.dgvColumns,
                    () => new GilbarcoColumnSettings
                    {
                        ColumnQuantity = selectedQuantity,
                        PriceDecimalPoint = PriceDecimalPoint.Two
                    });
            };
            view.btnDeleteColumn.Click += (s, e) => DeleteSelectedColumn(controller, view.dgvColumns);
            view.btnSave.Click += async (s, e) =>
            {
                ReadPortAndBaudRate(view.cmbPort, view.cmbBaudRate, controller);
                await _hardwareConfigurationService.SaveControllerAsync(controller);
            };

            _current.OnShow();
            _view.ShowContent(_current.View);
        }

        #endregion

        #region Private Methods — Emulator

        private void NavigateToEmulator(Controller controller)
        {
            _current = _pages.Create(PageType.Emulator);
            var view = (EmulatorView)_current.View;

            // Таблица колонок
            SetupEmulatorGrid(view.dgvColumns, controller);

            // Кнопки
            view.btnAddColumn.Click += (s, e) => AddColumnsToController(controller, view.dgvColumns,
                () => new LanfengColumnSettings());
            view.btnDeleteColumn.Click += (s, e) => DeleteSelectedColumn(controller, view.dgvColumns);
            view.btnSave.Click += async (s, e) =>
            {
                if (controller.Columns.Count == 0) return;

                foreach (var item in controller.Columns)
                {
                    if (item.SystemCounter < 0)
                    {
                        MessageBox.Show($"Системный счётчик колонки \"{item.Name}\" не может быть отрицательным.",
                            "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
                await _hardwareConfigurationService.SaveControllerAsync(controller);
            };

            _current.OnShow();
            _view.ShowContent(_current.View);
        }

        #endregion

        #region Private Methods — EKassa

        private void NavigateToEKassa(CashRegister cashRegister)
        {
            _current = _pages.Create(PageType.EKassa);
            var view = (EKassaView)_current.View;

            // Заполняем поля
            view.txtAddress.Text = cashRegister.Address ?? "";
            view.txtRegistrationNumber.Text = cashRegister.RegistrationNumber ?? "";
            view.txtUserName.Text = cashRegister.UserName ?? "";
            view.txtPassword.Text = cashRegister.Password ?? "";

            // Принтеры
            FillPrinters(view.cmbPrinter);
            if (cashRegister.Settings is EKassaCashRegisterSettings ekSettings)
            {
                if (!string.IsNullOrEmpty(ekSettings.DefaultPrinterName))
                    view.cmbPrinter.SelectedItem = ekSettings.DefaultPrinterName;

                // TapeType
                var tapeTypes = Enum.GetValues(typeof(TapeType)).Cast<TapeType>().ToList();
                view.cmbTapeType.DataSource = tapeTypes;
                view.cmbTapeType.SelectedItem = ekSettings.TapeType;
            }

            // Кнопки
            view.btnState.Click += async (s, e) =>
            {
                try
                {
                    ReadCashRegisterFields(view, cashRegister);
                    await _hardwareConfigurationService.SaveCashRegisterAsync(cashRegister);
                    // TODO: ICashRegisterFactory integration
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            view.btnOpenShift.Click += async (s, e) =>
            {
                try
                {
                    ReadCashRegisterFields(view, cashRegister);
                    await _hardwareConfigurationService.SaveCashRegisterAsync(cashRegister);
                    // TODO: ICashRegisterFactory integration
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            view.btnCloseShift.Click += async (s, e) =>
            {
                try
                {
                    ReadCashRegisterFields(view, cashRegister);
                    await _hardwareConfigurationService.SaveCashRegisterAsync(cashRegister);
                    // TODO: ICashRegisterFactory integration
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            view.btnSave.Click += async (s, e) =>
            {
                ReadCashRegisterFields(view, cashRegister);
                if (ValidateEKassa(cashRegister))
                {
                    await _hardwareConfigurationService.SaveCashRegisterAsync(cashRegister);
                }
            };

            _current.OnShow();
            _view.ShowContent(_current.View);
        }

        private void ReadCashRegisterFields(EKassaView view, CashRegister cashRegister)
        {
            cashRegister.Address = view.txtAddress.Text;
            cashRegister.RegistrationNumber = view.txtRegistrationNumber.Text;
            cashRegister.UserName = view.txtUserName.Text;
            cashRegister.Password = view.txtPassword.Text;
        }

        private bool ValidateEKassa(CashRegister cr)
        {
            if (string.IsNullOrEmpty(cr.Address))
            {
                MessageBox.Show("Введите адрес!", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return false;
            }
            if (string.IsNullOrEmpty(cr.RegistrationNumber))
            {
                MessageBox.Show("Введите регистрационный номер устройства!", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return false;
            }
            if (string.IsNullOrEmpty(cr.UserName))
            {
                MessageBox.Show("Введите пользователя!", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return false;
            }
            if (string.IsNullOrEmpty(cr.Password))
            {
                MessageBox.Show("Введите пароль!", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return false;
            }
            if (!Uri.IsWellFormedUriString(cr.Address, UriKind.Absolute))
            {
                MessageBox.Show("Адрес неправильно заполнен!", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return false;
            }
            return true;
        }

        #endregion

        #region Private Methods — NewCas

        private void NavigateToNewCas(CashRegister cashRegister)
        {
            _current = _pages.Create(PageType.NewCas);
            var view = (NewCasView)_current.View;

            view.txtAddress.Text = cashRegister.Address ?? "";
            view.txtRegistrationNumber.Text = cashRegister.RegistrationNumber ?? "";

            // Принтеры
            FillPrinters(view.cmbPrinter);
            if (cashRegister.Settings is NewCasCashRegisterSettings ncSettings)
            {
                if (!string.IsNullOrEmpty(ncSettings.DefaultPrinterName))
                    view.cmbPrinter.SelectedItem = ncSettings.DefaultPrinterName;
            }

            view.btnSave.Click += async (s, e) =>
            {
                cashRegister.Address = view.txtAddress.Text;
                cashRegister.RegistrationNumber = view.txtRegistrationNumber.Text;

                if (string.IsNullOrEmpty(cashRegister.Address))
                {
                    MessageBox.Show("Введите адрес!", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }
                if (!Uri.IsWellFormedUriString(cashRegister.Address, UriKind.Absolute))
                {
                    MessageBox.Show("Адрес неправильно заполнен!", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }

                await _hardwareConfigurationService.SaveCashRegisterAsync(cashRegister);
            };

            _current.OnShow();
            _view.ShowContent(_current.View);
        }

        #endregion

        #region Private Methods — Device Creation

        private void View_AddFuelDispenserClicked(object? sender, EventArgs e)
        {
            IDeviceService<Controller> controllerService = new ControllerService(_hardwareConfigurationService);
            var model = new HardwareModel<Controller, ControllerType>(controllerService);

            _dialogService.Show<HardwareDialog, HardwareModel<Controller, ControllerType>>(model);
        }

        private void View_AddCashRegisterClicked(object? sender, EventArgs e)
        {
            IDeviceService<CashRegister> cashRegisterService = new CashRegisterService(_hardwareConfigurationService);
            var model = new HardwareModel<CashRegister, CashRegisterType>(cashRegisterService);

            _dialogService.Show<HardwareDialog, HardwareModel<CashRegister, CashRegisterType>>(model);
        }

        #endregion

        #region Private Methods — Configuration Events

        private void OnControllerPropertyChanged(Controller controller)
        {
            var existing = _controllers.FirstOrDefault(c => c.Id == controller.Id);
            if (existing != null)
            {
                existing.Update(controller);
                // Обновляем текст узла в дереве
                UpdateTreeNodeText(_view.ControllersNode, controller.Id, controller.Name);
            }
            else
            {
                _controllers.Add(controller);
                AddControllerToTree(controller);
            }
        }

        private void OnCashRegisterPropertyChanged(CashRegister cashRegister)
        {
            var existing = _cashRegisters.FirstOrDefault(c => c.Id == cashRegister.Id);
            if (existing != null)
            {
                existing.Update(cashRegister);
                UpdateTreeNodeText(_view.CashRegistersNode, cashRegister.Id, cashRegister.Name);
            }
            else
            {
                _cashRegisters.Add(cashRegister);
                AddCashRegisterToTree(cashRegister);
            }
        }

        private void UpdateTreeNodeText(TreeNode parentNode, Guid id, string name)
        {
            foreach (TreeNode node in parentNode.Nodes)
            {
                if (node.Tag is Controller c && c.Id == id)
                {
                    node.Text = name;
                    return;
                }
                if (node.Tag is CashRegister cr && cr.Id == id)
                {
                    node.Text = name;
                    return;
                }
            }
        }

        #endregion

        #region Private Helpers

        private void ClearContent()
        {
            _current?.Dispose();
            _current = null;
            _view.ShowContent(new Panel());
        }

        private void FillPortsCombo(ComboBox cmb, string? selectedPort)
        {
            cmb.Items.Clear();
            foreach (var port in SerialPort.GetPortNames())
                cmb.Items.Add(port);
            if (!string.IsNullOrEmpty(selectedPort))
                cmb.SelectedItem = selectedPort;
        }

        private void FillCombo(ComboBox cmb, int[] values, int selectedValue)
        {
            cmb.Items.Clear();
            foreach (var v in values)
                cmb.Items.Add(v);
            cmb.SelectedItem = selectedValue;
        }

        private void FillPrinters(ComboBox cmb)
        {
            cmb.Items.Clear();
            cmb.Items.Add("Не задан");
            try
            {
                foreach (string printer in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
                {
                    cmb.Items.Add(printer);
                }
            }
            catch { }
        }

        private void ReadPortAndBaudRate(ComboBox cmbPort, ComboBox cmbBaudRate, Controller controller)
        {
            controller.ComPort = cmbPort.SelectedItem?.ToString() ?? "";
            if (cmbBaudRate.SelectedItem is int baudRate)
                controller.BaudRate = baudRate;
        }

        private void SetupFuelDispenserGrid(DataGridView dgv, Controller controller, bool isLanfeng)
        {
            dgv.Columns.Clear();
            dgv.Rows.Clear();

            dgv.Columns.Add("Address", "Адрес");
            dgv.Columns.Add("Nozzle", "Шланг");
            dgv.Columns.Add("Name", "Имя");

            var chkCol = new DataGridViewCheckBoxColumn
            {
                Name = "IsDisabled",
                HeaderText = "Блокирована"
            };
            dgv.Columns.Add(chkCol);

            foreach (var column in controller.Columns)
            {
                var rowIdx = dgv.Rows.Add(
                    column.Address,
                    column.Nozzle,
                    column.Name,
                    column.Settings?.IsDisabled ?? false
                );
                dgv.Rows[rowIdx].Tag = column;
            }

            // Подписка на изменение ячеек для записи обратно в модель
            dgv.CellValueChanged += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
                var row = dgv.Rows[e.RowIndex];
                if (row.Tag is not Column col) return;

                switch (dgv.Columns[e.ColumnIndex].Name)
                {
                    case "Address":
                        if (int.TryParse(row.Cells["Address"].Value?.ToString(), out int addr))
                            col.Address = addr;
                        break;
                    case "Nozzle":
                        if (int.TryParse(row.Cells["Nozzle"].Value?.ToString(), out int nozzle))
                            col.Nozzle = nozzle;
                        break;
                    case "Name":
                        col.Name = row.Cells["Name"].Value?.ToString() ?? "";
                        break;
                    case "IsDisabled":
                        if (col.Settings != null)
                            col.Settings.IsDisabled = (bool)(row.Cells["IsDisabled"].Value ?? false);
                        break;
                }
            };
        }

        private void SetupEmulatorGrid(DataGridView dgv, Controller controller)
        {
            dgv.Columns.Clear();
            dgv.Rows.Clear();

            dgv.Columns.Add("Address", "Адрес");
            dgv.Columns.Add("Nozzle", "Шланг");
            dgv.Columns.Add("Name", "Имя");

            var chkCol = new DataGridViewCheckBoxColumn
            {
                Name = "IsDisabled",
                HeaderText = "Блокирована"
            };
            dgv.Columns.Add(chkCol);

            dgv.Columns.Add("SystemCounter", "Системный счетчик");

            foreach (var column in controller.Columns)
            {
                var rowIdx = dgv.Rows.Add(
                    column.Address,
                    column.Nozzle,
                    column.Name,
                    column.Settings?.IsDisabled ?? false,
                    column.SystemCounter
                );
                dgv.Rows[rowIdx].Tag = column;
            }

            dgv.CellValueChanged += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
                var row = dgv.Rows[e.RowIndex];
                if (row.Tag is not Column col) return;

                switch (dgv.Columns[e.ColumnIndex].Name)
                {
                    case "Address":
                        if (int.TryParse(row.Cells["Address"].Value?.ToString(), out int addr))
                            col.Address = addr;
                        break;
                    case "Nozzle":
                        if (int.TryParse(row.Cells["Nozzle"].Value?.ToString(), out int nozzle))
                            col.Nozzle = nozzle;
                        break;
                    case "Name":
                        col.Name = row.Cells["Name"].Value?.ToString() ?? "";
                        break;
                    case "IsDisabled":
                        if (col.Settings != null)
                            col.Settings.IsDisabled = (bool)(row.Cells["IsDisabled"].Value ?? false);
                        break;
                    case "SystemCounter":
                        if (decimal.TryParse(row.Cells["SystemCounter"].Value?.ToString(), out decimal sc))
                            col.SystemCounter = sc;
                        break;
                }
            };
        }

        private void AddColumnsToController(Controller controller, DataGridView dgv, Func<ColumnSettings> createSettings)
        {
            using var dlg = new ColumnCountDialog();
            if (dlg.ShowDialog() != DialogResult.OK || dlg.ColumnCount <= 0) return;

            int newColumnsCount = dlg.ColumnCount;
            int currentCount = controller.Columns.Count;

            for (int i = 0; i < newColumnsCount; i++)
            {
                int totalIndex = currentCount + i;
                int address = totalIndex / 4;
                int pistol = (totalIndex % 4) + 1;
                string name = $"Колонка_{controller.Columns.Count + 1}";

                var newColumn = new Column
                {
                    Address = address,
                    Nozzle = pistol,
                    Name = name,
                    Settings = createSettings()
                };

                controller.Columns.Add(newColumn);

                var rowIdx = dgv.Rows.Add(
                    newColumn.Address,
                    newColumn.Nozzle,
                    newColumn.Name,
                    newColumn.Settings?.IsDisabled ?? false
                );
                dgv.Rows[rowIdx].Tag = newColumn;
            }
        }

        private void DeleteSelectedColumn(Controller controller, DataGridView dgv)
        {
            if (dgv.SelectedRows.Count == 0) return;

            var result = MessageBox.Show("Удалить колонку?", "Подтверждение",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.No) return;

            var row = dgv.SelectedRows[0];
            if (row.Tag is Column column)
            {
                controller.Columns.Remove(column);
            }
            dgv.Rows.Remove(row);
        }

        #endregion
    }
}
