namespace KIT.GasStation.HardwareSettings.CustomControl.Views
{
    partial class LanfengView
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
            tlpTop = new TableLayoutPanel();
            tlpPortSettings = new TableLayoutPanel();
            label1 = new Label();
            cmbPort = new ComboBox();
            label2 = new Label();
            cmbBaudrate = new ComboBox();
            btnSettings = new Button();
            groupBox1 = new GroupBox();
            tableLayoutPanel3 = new TableLayoutPanel();
            checkBox2 = new CheckBox();
            checkBox1 = new CheckBox();
            dgvColumns = new DataGridView();
            tlpButtons = new TableLayoutPanel();
            btnAddColumn = new Button();
            btnDeleteColumn = new Button();
            btnSave = new Button();
            tlpMain.SuspendLayout();
            tlpTop.SuspendLayout();
            tlpPortSettings.SuspendLayout();
            groupBox1.SuspendLayout();
            tableLayoutPanel3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvColumns).BeginInit();
            tlpButtons.SuspendLayout();
            SuspendLayout();
            //
            // tlpMain
            //
            tlpMain.ColumnCount = 1;
            tlpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpMain.Controls.Add(tlpTop, 0, 0);
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
            // tlpTop
            //
            tlpTop.ColumnCount = 2;
            tlpTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpTop.Controls.Add(tlpPortSettings, 0, 0);
            tlpTop.Controls.Add(groupBox1, 1, 0);
            tlpTop.Dock = DockStyle.Fill;
            tlpTop.AutoSize = true;
            tlpTop.Location = new Point(0, 0);
            tlpTop.Name = "tlpTop";
            tlpTop.RowCount = 1;
            tlpTop.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlpTop.Size = new Size(1140, 100);
            tlpTop.TabIndex = 0;
            //
            // tlpPortSettings
            //
            tlpPortSettings.ColumnCount = 2;
            tlpPortSettings.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tlpPortSettings.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpPortSettings.Controls.Add(label1, 0, 0);
            tlpPortSettings.Controls.Add(cmbPort, 1, 0);
            tlpPortSettings.Controls.Add(label2, 0, 1);
            tlpPortSettings.Controls.Add(cmbBaudrate, 1, 1);
            tlpPortSettings.Controls.Add(btnSettings, 0, 2);
            tlpPortSettings.SetColumnSpan(btnSettings, 2);
            tlpPortSettings.Dock = DockStyle.Fill;
            tlpPortSettings.Location = new Point(3, 3);
            tlpPortSettings.Name = "tlpPortSettings";
            tlpPortSettings.RowCount = 3;
            tlpPortSettings.RowStyles.Add(new RowStyle(SizeType.Percent, 33F));
            tlpPortSettings.RowStyles.Add(new RowStyle(SizeType.Percent, 33F));
            tlpPortSettings.RowStyles.Add(new RowStyle(SizeType.Percent, 34F));
            tlpPortSettings.Size = new Size(564, 90);
            tlpPortSettings.TabIndex = 0;
            //
            // label1
            //
            label1.Anchor = AnchorStyles.Left;
            label1.AutoSize = true;
            label1.Location = new Point(3, 6);
            label1.Name = "label1";
            label1.Size = new Size(35, 15);
            label1.TabIndex = 0;
            label1.Text = "Порт";
            label1.TextAlign = ContentAlignment.MiddleLeft;
            //
            // cmbPort
            //
            cmbPort.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            cmbPort.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbPort.FormattingEnabled = true;
            cmbPort.Location = new Point(68, 3);
            cmbPort.Name = "cmbPort";
            cmbPort.Size = new Size(493, 23);
            cmbPort.TabIndex = 1;
            //
            // label2
            //
            label2.Anchor = AnchorStyles.Left;
            label2.AutoSize = true;
            label2.Location = new Point(3, 35);
            label2.Name = "label2";
            label2.Size = new Size(59, 15);
            label2.TabIndex = 2;
            label2.Text = "Скорость";
            label2.TextAlign = ContentAlignment.MiddleLeft;
            //
            // cmbBaudrate
            //
            cmbBaudrate.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            cmbBaudrate.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbBaudrate.FormattingEnabled = true;
            cmbBaudrate.Location = new Point(68, 31);
            cmbBaudrate.Name = "cmbBaudrate";
            cmbBaudrate.Size = new Size(493, 23);
            cmbBaudrate.TabIndex = 3;
            //
            // btnSettings
            //
            btnSettings.AutoSize = true;
            btnSettings.Text = "Настройки ТРК";
            btnSettings.Location = new Point(3, 62);
            btnSettings.Name = "btnSettings";
            btnSettings.Padding = new Padding(5);
            btnSettings.TabIndex = 4;
            //
            // groupBox1
            //
            groupBox1.Controls.Add(tableLayoutPanel3);
            groupBox1.Dock = DockStyle.Fill;
            groupBox1.Location = new Point(573, 3);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(564, 90);
            groupBox1.TabIndex = 1;
            groupBox1.TabStop = false;
            groupBox1.Text = "Лог";
            //
            // tableLayoutPanel3
            //
            tableLayoutPanel3.ColumnCount = 1;
            tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel3.Controls.Add(checkBox2, 0, 1);
            tableLayoutPanel3.Controls.Add(checkBox1, 0, 0);
            tableLayoutPanel3.Dock = DockStyle.Top;
            tableLayoutPanel3.Location = new Point(3, 19);
            tableLayoutPanel3.Name = "tableLayoutPanel3";
            tableLayoutPanel3.RowCount = 2;
            tableLayoutPanel3.RowStyles.Add(new RowStyle());
            tableLayoutPanel3.RowStyles.Add(new RowStyle());
            tableLayoutPanel3.Size = new Size(558, 50);
            tableLayoutPanel3.TabIndex = 0;
            //
            // checkBox2
            //
            checkBox2.AutoSize = true;
            checkBox2.CheckAlign = ContentAlignment.MiddleRight;
            checkBox2.Location = new Point(3, 28);
            checkBox2.Name = "checkBox2";
            checkBox2.Size = new Size(121, 19);
            checkBox2.TabIndex = 1;
            checkBox2.Text = "Низкоуровневый";
            checkBox2.UseVisualStyleBackColor = true;
            //
            // checkBox1
            //
            checkBox1.AutoSize = true;
            checkBox1.CheckAlign = ContentAlignment.MiddleRight;
            checkBox1.Location = new Point(3, 3);
            checkBox1.Name = "checkBox1";
            checkBox1.Size = new Size(81, 19);
            checkBox1.TabIndex = 0;
            checkBox1.Text = "Пакетный";
            checkBox1.UseVisualStyleBackColor = true;
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
            // LanfengView
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(tlpMain);
            Name = "LanfengView";
            Size = new Size(1140, 734);
            tlpMain.ResumeLayout(false);
            tlpMain.PerformLayout();
            tlpTop.ResumeLayout(false);
            tlpPortSettings.ResumeLayout(false);
            tlpPortSettings.PerformLayout();
            groupBox1.ResumeLayout(false);
            tableLayoutPanel3.ResumeLayout(false);
            tableLayoutPanel3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvColumns).EndInit();
            tlpButtons.ResumeLayout(false);
            tlpButtons.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel tlpMain;
        private TableLayoutPanel tlpTop;
        private TableLayoutPanel tlpPortSettings;
        private Label label1;
        public ComboBox cmbPort;
        private Label label2;
        public ComboBox cmbBaudrate;
        public Button btnSettings;
        private GroupBox groupBox1;
        private TableLayoutPanel tableLayoutPanel3;
        public CheckBox checkBox2;
        public CheckBox checkBox1;
        public DataGridView dgvColumns;
        private TableLayoutPanel tlpButtons;
        public Button btnAddColumn;
        public Button btnDeleteColumn;
        public Button btnSave;
    }
}
