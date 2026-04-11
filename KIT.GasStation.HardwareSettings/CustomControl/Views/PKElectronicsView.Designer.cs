namespace KIT.GasStation.HardwareSettings.CustomControl.Views
{
    partial class PKElectronicsView
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

        private void InitializeComponent()
        {
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.panelSettings = new System.Windows.Forms.Panel();
            this.groupBoxPort = new System.Windows.Forms.GroupBox();
            this.tableLayoutPortSettings = new System.Windows.Forms.TableLayoutPanel();
            this.labelPort = new System.Windows.Forms.Label();
            this.cmbPort = new System.Windows.Forms.ComboBox();
            this.labelBaudRate = new System.Windows.Forms.Label();
            this.cmbBaudRate = new System.Windows.Forms.ComboBox();
            this.groupBoxPolling = new System.Windows.Forms.GroupBox();
            this.tableLayoutPollingSettings = new System.Windows.Forms.TableLayoutPanel();
            this.labelPollingMode = new System.Windows.Forms.Label();
            this.cmbPollingMode = new System.Windows.Forms.ComboBox();
            this.labelNozzlesPerSide = new System.Windows.Forms.Label();
            this.cmbNozzlesPerSide = new System.Windows.Forms.ComboBox();
            this.dgvColumns = new System.Windows.Forms.DataGridView();
            this.panelButtons = new System.Windows.Forms.Panel();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnDeleteColumn = new System.Windows.Forms.Button();
            this.btnAddColumn = new System.Windows.Forms.Button();

            this.tableLayoutPanel1.SuspendLayout();
            this.panelSettings.SuspendLayout();
            this.groupBoxPort.SuspendLayout();
            this.tableLayoutPortSettings.SuspendLayout();
            this.groupBoxPolling.SuspendLayout();
            this.tableLayoutPollingSettings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvColumns)).BeginInit();
            this.panelButtons.SuspendLayout();
            this.SuspendLayout();

            // tableLayoutPanel1
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.panelSettings, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.dgvColumns, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.panelButtons, 0, 2);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tableLayoutPanel1.TabIndex = 0;

            // panelSettings
            this.panelSettings.AutoSize = true;
            this.panelSettings.Controls.Add(this.groupBoxPort);
            this.panelSettings.Controls.Add(this.groupBoxPolling);
            this.panelSettings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelSettings.Name = "panelSettings";
            this.panelSettings.TabIndex = 0;

            // groupBoxPort
            this.groupBoxPort.AutoSize = true;
            this.groupBoxPort.Controls.Add(this.tableLayoutPortSettings);
            this.groupBoxPort.Dock = System.Windows.Forms.DockStyle.Left;
            this.groupBoxPort.Location = new System.Drawing.Point(0, 0);
            this.groupBoxPort.Name = "groupBoxPort";
            this.groupBoxPort.Padding = new System.Windows.Forms.Padding(10);
            this.groupBoxPort.Size = new System.Drawing.Size(300, 100);
            this.groupBoxPort.TabIndex = 0;
            this.groupBoxPort.TabStop = false;
            this.groupBoxPort.Text = "Порт";

            // tableLayoutPortSettings
            this.tableLayoutPortSettings.AutoSize = true;
            this.tableLayoutPortSettings.ColumnCount = 2;
            this.tableLayoutPortSettings.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tableLayoutPortSettings.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPortSettings.Controls.Add(this.labelPort, 0, 0);
            this.tableLayoutPortSettings.Controls.Add(this.cmbPort, 1, 0);
            this.tableLayoutPortSettings.Controls.Add(this.labelBaudRate, 0, 1);
            this.tableLayoutPortSettings.Controls.Add(this.cmbBaudRate, 1, 1);
            this.tableLayoutPortSettings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPortSettings.Name = "tableLayoutPortSettings";
            this.tableLayoutPortSettings.RowCount = 2;
            this.tableLayoutPortSettings.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tableLayoutPortSettings.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tableLayoutPortSettings.TabIndex = 0;

            // labelPort
            this.labelPort.AutoSize = true;
            this.labelPort.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelPort.Location = new System.Drawing.Point(3, 3);
            this.labelPort.Name = "labelPort";
            this.labelPort.Size = new System.Drawing.Size(60, 25);
            this.labelPort.TabIndex = 0;
            this.labelPort.Text = "Порт:";
            this.labelPort.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // cmbPort
            this.cmbPort.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cmbPort.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPort.Location = new System.Drawing.Point(70, 3);
            this.cmbPort.Name = "cmbPort";
            this.cmbPort.Size = new System.Drawing.Size(200, 25);
            this.cmbPort.TabIndex = 1;

            // labelBaudRate
            this.labelBaudRate.AutoSize = true;
            this.labelBaudRate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelBaudRate.Location = new System.Drawing.Point(3, 33);
            this.labelBaudRate.Name = "labelBaudRate";
            this.labelBaudRate.Size = new System.Drawing.Size(60, 25);
            this.labelBaudRate.TabIndex = 2;
            this.labelBaudRate.Text = "Скорость:";
            this.labelBaudRate.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // cmbBaudRate
            this.cmbBaudRate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cmbBaudRate.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbBaudRate.Location = new System.Drawing.Point(70, 33);
            this.cmbBaudRate.Name = "cmbBaudRate";
            this.cmbBaudRate.Size = new System.Drawing.Size(200, 25);
            this.cmbBaudRate.TabIndex = 3;

            // groupBoxPolling
            this.groupBoxPolling.AutoSize = true;
            this.groupBoxPolling.Controls.Add(this.tableLayoutPollingSettings);
            this.groupBoxPolling.Dock = System.Windows.Forms.DockStyle.Left;
            this.groupBoxPolling.Location = new System.Drawing.Point(300, 0);
            this.groupBoxPolling.Name = "groupBoxPolling";
            this.groupBoxPolling.Padding = new System.Windows.Forms.Padding(10);
            this.groupBoxPolling.Size = new System.Drawing.Size(300, 100);
            this.groupBoxPolling.TabIndex = 1;
            this.groupBoxPolling.TabStop = false;
            this.groupBoxPolling.Text = "Опрос";

            // tableLayoutPollingSettings
            this.tableLayoutPollingSettings.AutoSize = true;
            this.tableLayoutPollingSettings.ColumnCount = 2;
            this.tableLayoutPollingSettings.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tableLayoutPollingSettings.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPollingSettings.Controls.Add(this.labelPollingMode, 0, 0);
            this.tableLayoutPollingSettings.Controls.Add(this.cmbPollingMode, 1, 0);
            this.tableLayoutPollingSettings.Controls.Add(this.labelNozzlesPerSide, 0, 1);
            this.tableLayoutPollingSettings.Controls.Add(this.cmbNozzlesPerSide, 1, 1);
            this.tableLayoutPollingSettings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPollingSettings.Name = "tableLayoutPollingSettings";
            this.tableLayoutPollingSettings.RowCount = 2;
            this.tableLayoutPollingSettings.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tableLayoutPollingSettings.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tableLayoutPollingSettings.TabIndex = 0;

            // labelPollingMode
            this.labelPollingMode.AutoSize = true;
            this.labelPollingMode.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelPollingMode.Location = new System.Drawing.Point(3, 3);
            this.labelPollingMode.Name = "labelPollingMode";
            this.labelPollingMode.Size = new System.Drawing.Size(100, 25);
            this.labelPollingMode.TabIndex = 0;
            this.labelPollingMode.Text = "Режим опроса:";
            this.labelPollingMode.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // cmbPollingMode
            this.cmbPollingMode.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cmbPollingMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPollingMode.Location = new System.Drawing.Point(110, 3);
            this.cmbPollingMode.Name = "cmbPollingMode";
            this.cmbPollingMode.Size = new System.Drawing.Size(180, 25);
            this.cmbPollingMode.TabIndex = 1;

            // labelNozzlesPerSide
            this.labelNozzlesPerSide.AutoSize = true;
            this.labelNozzlesPerSide.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelNozzlesPerSide.Location = new System.Drawing.Point(3, 33);
            this.labelNozzlesPerSide.Name = "labelNozzlesPerSide";
            this.labelNozzlesPerSide.Size = new System.Drawing.Size(100, 25);
            this.labelNozzlesPerSide.TabIndex = 2;
            this.labelNozzlesPerSide.Text = "Колонок на сторону:";
            this.labelNozzlesPerSide.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // cmbNozzlesPerSide
            this.cmbNozzlesPerSide.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cmbNozzlesPerSide.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbNozzlesPerSide.Location = new System.Drawing.Point(110, 33);
            this.cmbNozzlesPerSide.Name = "cmbNozzlesPerSide";
            this.cmbNozzlesPerSide.Size = new System.Drawing.Size(180, 25);
            this.cmbNozzlesPerSide.TabIndex = 3;

            // dgvColumns
            this.dgvColumns.AllowUserToAddRows = false;
            this.dgvColumns.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvColumns.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvColumns.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvColumns.Location = new System.Drawing.Point(3, 103);
            this.dgvColumns.Name = "dgvColumns";
            this.dgvColumns.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvColumns.Size = new System.Drawing.Size(594, 200);
            this.dgvColumns.TabIndex = 1;

            // panelButtons
            this.panelButtons.AutoSize = true;
            this.panelButtons.Controls.Add(this.btnSave);
            this.panelButtons.Controls.Add(this.btnDeleteColumn);
            this.panelButtons.Controls.Add(this.btnAddColumn);
            this.panelButtons.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelButtons.Name = "panelButtons";
            this.panelButtons.Padding = new System.Windows.Forms.Padding(10);
            this.panelButtons.TabIndex = 2;

            // btnAddColumn
            this.btnAddColumn.Location = new System.Drawing.Point(13, 13);
            this.btnAddColumn.Name = "btnAddColumn";
            this.btnAddColumn.Size = new System.Drawing.Size(150, 30);
            this.btnAddColumn.TabIndex = 0;
            this.btnAddColumn.Text = "Добавить колонку";
            this.btnAddColumn.UseVisualStyleBackColor = true;

            // btnDeleteColumn
            this.btnDeleteColumn.Location = new System.Drawing.Point(169, 13);
            this.btnDeleteColumn.Name = "btnDeleteColumn";
            this.btnDeleteColumn.Size = new System.Drawing.Size(150, 30);
            this.btnDeleteColumn.TabIndex = 1;
            this.btnDeleteColumn.Text = "Удалить колонку";
            this.btnDeleteColumn.UseVisualStyleBackColor = true;

            // btnSave
            this.btnSave.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.btnSave.Location = new System.Drawing.Point(447, 13);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(150, 30);
            this.btnSave.TabIndex = 2;
            this.btnSave.Text = "Сохранить настройки";
            this.btnSave.UseVisualStyleBackColor = true;

            // PKElectronicsView
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "PKElectronicsView";
            this.Size = new System.Drawing.Size(600, 400);

            this.tableLayoutPanel1.ResumeLayout(false);
            this.panelSettings.ResumeLayout(false);
            this.groupBoxPort.ResumeLayout(false);
            this.tableLayoutPortSettings.ResumeLayout(false);
            this.groupBoxPolling.ResumeLayout(false);
            this.tableLayoutPollingSettings.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvColumns)).EndInit();
            this.panelButtons.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        public System.Windows.Forms.ComboBox cmbPort;
        public System.Windows.Forms.ComboBox cmbBaudRate;
        public System.Windows.Forms.ComboBox cmbPollingMode;
        public System.Windows.Forms.ComboBox cmbNozzlesPerSide;
        public System.Windows.Forms.DataGridView dgvColumns;
        public System.Windows.Forms.Button btnAddColumn;
        public System.Windows.Forms.Button btnDeleteColumn;
        public System.Windows.Forms.Button btnSave;

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Panel panelSettings;
        private System.Windows.Forms.GroupBox groupBoxPort;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPortSettings;
        private System.Windows.Forms.Label labelPort;
        private System.Windows.Forms.Label labelBaudRate;
        private System.Windows.Forms.GroupBox groupBoxPolling;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPollingSettings;
        private System.Windows.Forms.Label labelPollingMode;
        private System.Windows.Forms.Label labelNozzlesPerSide;
        private System.Windows.Forms.Panel panelButtons;
    }
}
