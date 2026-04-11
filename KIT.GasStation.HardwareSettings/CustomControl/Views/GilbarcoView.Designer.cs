using System.IO.Ports;

namespace KIT.GasStation.HardwareSettings.CustomControl.Views
{
    partial class GilbarcoView
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        private void InitializeComponent()
        {
            tlpMain = new TableLayoutPanel();
            tlpSettings = new TableLayoutPanel();
            lblPort = new Label();
            cmbPort = new ComboBox();
            lblBaudRate = new Label();
            cmbBaudRate = new ComboBox();
            lblParity = new Label();
            cmbParity = new ComboBox();
            lblColumnQuantity = new Label();
            cmbColumnQuantity = new ComboBox();
            grpLog = new GroupBox();
            tlpLog = new TableLayoutPanel();
            chkPacketLog = new CheckBox();
            chkLowLevelLog = new CheckBox();
            tlpEcho = new TableLayoutPanel();
            chkEchoSuppression = new CheckBox();
            dgvColumns = new DataGridView();
            tlpButtons = new TableLayoutPanel();
            btnAddColumn = new Button();
            btnDeleteColumn = new Button();
            btnSave = new Button();
            tlpMain.SuspendLayout();
            tlpSettings.SuspendLayout();
            grpLog.SuspendLayout();
            tlpLog.SuspendLayout();
            tlpEcho.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvColumns).BeginInit();
            tlpButtons.SuspendLayout();
            SuspendLayout();
            //
            // tlpMain
            //
            tlpMain.ColumnCount = 1;
            tlpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpMain.Controls.Add(tlpSettings, 0, 0);
            tlpMain.Controls.Add(dgvColumns, 0, 1);
            tlpMain.Controls.Add(tlpButtons, 0, 2);
            tlpMain.Dock = DockStyle.Fill;
            tlpMain.Location = new Point(0, 0);
            tlpMain.Name = "tlpMain";
            tlpMain.RowCount = 3;
            tlpMain.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpMain.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlpMain.Size = new Size(1140, 734);
            tlpMain.TabIndex = 0;
            //
            // tlpSettings
            //
            tlpSettings.ColumnCount = 3;
            tlpSettings.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
            tlpSettings.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
            tlpSettings.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34F));
            tlpSettings.Controls.Add(CreateSettingsPanel(), 0, 0);
            tlpSettings.Controls.Add(tlpEcho, 1, 0);
            tlpSettings.Controls.Add(grpLog, 2, 0);
            tlpSettings.Dock = DockStyle.Fill;
            tlpSettings.AutoSize = true;
            tlpSettings.Location = new Point(0, 0);
            tlpSettings.Name = "tlpSettings";
            tlpSettings.RowCount = 1;
            tlpSettings.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlpSettings.Size = new Size(1140, 120);
            tlpSettings.TabIndex = 0;
            //
            // grpLog
            //
            grpLog.Controls.Add(tlpLog);
            grpLog.Dock = DockStyle.Fill;
            grpLog.Text = "Лог";
            //
            // tlpLog
            //
            tlpLog.ColumnCount = 1;
            tlpLog.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpLog.Controls.Add(chkPacketLog, 0, 0);
            tlpLog.Controls.Add(chkLowLevelLog, 0, 1);
            tlpLog.Dock = DockStyle.Fill;
            tlpLog.RowCount = 2;
            tlpLog.RowStyles.Add(new RowStyle());
            tlpLog.RowStyles.Add(new RowStyle());
            //
            // chkPacketLog
            //
            chkPacketLog.AutoSize = true;
            chkPacketLog.Text = "Пакетный";
            //
            // chkLowLevelLog
            //
            chkLowLevelLog.AutoSize = true;
            chkLowLevelLog.Text = "Низкоуровневый";
            //
            // tlpEcho
            //
            tlpEcho.ColumnCount = 1;
            tlpEcho.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpEcho.Controls.Add(chkEchoSuppression, 0, 0);
            tlpEcho.Dock = DockStyle.Fill;
            tlpEcho.RowCount = 1;
            tlpEcho.RowStyles.Add(new RowStyle());
            //
            // chkEchoSuppression
            //
            chkEchoSuppression.AutoSize = true;
            chkEchoSuppression.Text = "Подавление эхо";
            //
            // dgvColumns
            //
            dgvColumns.Dock = DockStyle.Fill;
            dgvColumns.AllowUserToAddRows = false;
            dgvColumns.AllowUserToDeleteRows = false;
            dgvColumns.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvColumns.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvColumns.Name = "dgvColumns";
            //
            // tlpButtons
            //
            tlpButtons.ColumnCount = 4;
            tlpButtons.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tlpButtons.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tlpButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpButtons.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tlpButtons.Controls.Add(btnAddColumn, 0, 0);
            tlpButtons.Controls.Add(btnDeleteColumn, 1, 0);
            tlpButtons.Controls.Add(btnSave, 3, 0);
            tlpButtons.Dock = DockStyle.Fill;
            tlpButtons.AutoSize = true;
            tlpButtons.RowCount = 1;
            tlpButtons.RowStyles.Add(new RowStyle());
            //
            // btnAddColumn
            //
            btnAddColumn.Text = "Добавить колонку";
            btnAddColumn.AutoSize = true;
            btnAddColumn.Padding = new Padding(5);
            //
            // btnDeleteColumn
            //
            btnDeleteColumn.Text = "Удалить колонку";
            btnDeleteColumn.AutoSize = true;
            btnDeleteColumn.Padding = new Padding(5);
            //
            // btnSave
            //
            btnSave.Text = "Сохранить настройки";
            btnSave.AutoSize = true;
            btnSave.Padding = new Padding(5);
            //
            // GilbarcoView
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(tlpMain);
            Name = "GilbarcoView";
            Size = new Size(1140, 734);
            tlpMain.ResumeLayout(false);
            tlpMain.PerformLayout();
            grpLog.ResumeLayout(false);
            tlpLog.ResumeLayout(false);
            tlpLog.PerformLayout();
            tlpEcho.ResumeLayout(false);
            tlpEcho.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvColumns).EndInit();
            tlpButtons.ResumeLayout(false);
            tlpButtons.PerformLayout();
            ResumeLayout(false);
        }

        private TableLayoutPanel CreateSettingsPanel()
        {
            var tlp = new TableLayoutPanel();
            tlp.ColumnCount = 2;
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlp.RowCount = 4;
            tlp.RowStyles.Add(new RowStyle());
            tlp.RowStyles.Add(new RowStyle());
            tlp.RowStyles.Add(new RowStyle());
            tlp.RowStyles.Add(new RowStyle());
            tlp.Dock = DockStyle.Fill;

            lblPort = new Label { Text = "Порт", Anchor = AnchorStyles.Left, AutoSize = true };
            cmbPort = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            lblBaudRate = new Label { Text = "Скорость", Anchor = AnchorStyles.Left, AutoSize = true };
            cmbBaudRate = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            lblParity = new Label { Text = "Контроль", Anchor = AnchorStyles.Left, AutoSize = true };
            cmbParity = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            lblColumnQuantity = new Label { Text = "Кол-во пистолетов", Anchor = AnchorStyles.Left, AutoSize = true };
            cmbColumnQuantity = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };

            tlp.Controls.Add(lblPort, 0, 0);
            tlp.Controls.Add(cmbPort, 1, 0);
            tlp.Controls.Add(lblBaudRate, 0, 1);
            tlp.Controls.Add(cmbBaudRate, 1, 1);
            tlp.Controls.Add(lblParity, 0, 2);
            tlp.Controls.Add(cmbParity, 1, 2);
            tlp.Controls.Add(lblColumnQuantity, 0, 3);
            tlp.Controls.Add(cmbColumnQuantity, 1, 3);

            return tlp;
        }

        #endregion

        public TableLayoutPanel tlpMain;
        private TableLayoutPanel tlpSettings;
        private Label lblPort;
        public ComboBox cmbPort;
        private Label lblBaudRate;
        public ComboBox cmbBaudRate;
        private Label lblParity;
        public ComboBox cmbParity;
        private Label lblColumnQuantity;
        public ComboBox cmbColumnQuantity;
        private GroupBox grpLog;
        private TableLayoutPanel tlpLog;
        public CheckBox chkPacketLog;
        public CheckBox chkLowLevelLog;
        private TableLayoutPanel tlpEcho;
        public CheckBox chkEchoSuppression;
        public DataGridView dgvColumns;
        private TableLayoutPanel tlpButtons;
        public Button btnAddColumn;
        public Button btnDeleteColumn;
        public Button btnSave;
    }
}
