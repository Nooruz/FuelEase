namespace KIT.GasStation.HardwareSettings.CustomControl.Views
{
    partial class NewCasView
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
            this.panelAddress = new System.Windows.Forms.Panel();
            this.labelAddress = new System.Windows.Forms.Label();
            this.txtAddress = new System.Windows.Forms.TextBox();
            this.groupBoxCredentials = new System.Windows.Forms.GroupBox();
            this.tableLayoutCredentials = new System.Windows.Forms.TableLayoutPanel();
            this.labelRegistrationNumber = new System.Windows.Forms.Label();
            this.txtRegistrationNumber = new System.Windows.Forms.TextBox();
            this.groupBoxPrinter = new System.Windows.Forms.GroupBox();
            this.tableLayoutPrinter = new System.Windows.Forms.TableLayoutPanel();
            this.labelPrinter = new System.Windows.Forms.Label();
            this.cmbPrinter = new System.Windows.Forms.ComboBox();
            this.panelSave = new System.Windows.Forms.Panel();
            this.btnSave = new System.Windows.Forms.Button();

            this.tableLayoutPanel1.SuspendLayout();
            this.panelAddress.SuspendLayout();
            this.groupBoxCredentials.SuspendLayout();
            this.tableLayoutCredentials.SuspendLayout();
            this.groupBoxPrinter.SuspendLayout();
            this.tableLayoutPrinter.SuspendLayout();
            this.panelSave.SuspendLayout();
            this.SuspendLayout();

            // tableLayoutPanel1
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.panelAddress, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.groupBoxCredentials, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.groupBoxPrinter, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.panelSave, 0, 3);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 4;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tableLayoutPanel1.TabIndex = 0;
            this.tableLayoutPanel1.Padding = new System.Windows.Forms.Padding(10);

            // panelAddress
            this.panelAddress.AutoSize = true;
            this.panelAddress.Controls.Add(this.labelAddress);
            this.panelAddress.Controls.Add(this.txtAddress);
            this.panelAddress.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelAddress.Name = "panelAddress";
            this.panelAddress.TabIndex = 0;

            // labelAddress
            this.labelAddress.AutoSize = true;
            this.labelAddress.Location = new System.Drawing.Point(0, 5);
            this.labelAddress.Name = "labelAddress";
            this.labelAddress.Size = new System.Drawing.Size(140, 15);
            this.labelAddress.TabIndex = 0;
            this.labelAddress.Text = "Адрес смарт карты:";
            this.labelAddress.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // txtAddress
            this.txtAddress.Location = new System.Drawing.Point(150, 3);
            this.txtAddress.Name = "txtAddress";
            this.txtAddress.Size = new System.Drawing.Size(300, 23);
            this.txtAddress.TabIndex = 1;

            // groupBoxCredentials
            this.groupBoxCredentials.AutoSize = true;
            this.groupBoxCredentials.Controls.Add(this.tableLayoutCredentials);
            this.groupBoxCredentials.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBoxCredentials.Name = "groupBoxCredentials";
            this.groupBoxCredentials.Padding = new System.Windows.Forms.Padding(10);
            this.groupBoxCredentials.Size = new System.Drawing.Size(500, 80);
            this.groupBoxCredentials.TabIndex = 1;
            this.groupBoxCredentials.TabStop = false;
            this.groupBoxCredentials.Text = "Реквизиты подключения";

            // tableLayoutCredentials
            this.tableLayoutCredentials.AutoSize = true;
            this.tableLayoutCredentials.ColumnCount = 2;
            this.tableLayoutCredentials.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tableLayoutCredentials.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutCredentials.Controls.Add(this.labelRegistrationNumber, 0, 0);
            this.tableLayoutCredentials.Controls.Add(this.txtRegistrationNumber, 1, 0);
            this.tableLayoutCredentials.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutCredentials.Name = "tableLayoutCredentials";
            this.tableLayoutCredentials.RowCount = 1;
            this.tableLayoutCredentials.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tableLayoutCredentials.TabIndex = 0;

            // labelRegistrationNumber
            this.labelRegistrationNumber.AutoSize = true;
            this.labelRegistrationNumber.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelRegistrationNumber.Location = new System.Drawing.Point(3, 3);
            this.labelRegistrationNumber.Name = "labelRegistrationNumber";
            this.labelRegistrationNumber.Size = new System.Drawing.Size(180, 25);
            this.labelRegistrationNumber.TabIndex = 0;
            this.labelRegistrationNumber.Text = "Рег. номер устройства:";
            this.labelRegistrationNumber.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // txtRegistrationNumber
            this.txtRegistrationNumber.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtRegistrationNumber.Location = new System.Drawing.Point(190, 3);
            this.txtRegistrationNumber.Name = "txtRegistrationNumber";
            this.txtRegistrationNumber.Size = new System.Drawing.Size(290, 23);
            this.txtRegistrationNumber.TabIndex = 1;

            // groupBoxPrinter
            this.groupBoxPrinter.AutoSize = true;
            this.groupBoxPrinter.Controls.Add(this.tableLayoutPrinter);
            this.groupBoxPrinter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBoxPrinter.Name = "groupBoxPrinter";
            this.groupBoxPrinter.Padding = new System.Windows.Forms.Padding(10);
            this.groupBoxPrinter.Size = new System.Drawing.Size(500, 80);
            this.groupBoxPrinter.TabIndex = 2;
            this.groupBoxPrinter.TabStop = false;
            this.groupBoxPrinter.Text = "Принтер";

            // tableLayoutPrinter
            this.tableLayoutPrinter.AutoSize = true;
            this.tableLayoutPrinter.ColumnCount = 2;
            this.tableLayoutPrinter.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tableLayoutPrinter.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPrinter.Controls.Add(this.labelPrinter, 0, 0);
            this.tableLayoutPrinter.Controls.Add(this.cmbPrinter, 1, 0);
            this.tableLayoutPrinter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPrinter.Name = "tableLayoutPrinter";
            this.tableLayoutPrinter.RowCount = 1;
            this.tableLayoutPrinter.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tableLayoutPrinter.TabIndex = 0;

            // labelPrinter
            this.labelPrinter.AutoSize = true;
            this.labelPrinter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelPrinter.Location = new System.Drawing.Point(3, 3);
            this.labelPrinter.Name = "labelPrinter";
            this.labelPrinter.Size = new System.Drawing.Size(90, 25);
            this.labelPrinter.TabIndex = 0;
            this.labelPrinter.Text = "Принтер:";
            this.labelPrinter.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // cmbPrinter
            this.cmbPrinter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cmbPrinter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPrinter.Location = new System.Drawing.Point(100, 3);
            this.cmbPrinter.Name = "cmbPrinter";
            this.cmbPrinter.Size = new System.Drawing.Size(380, 25);
            this.cmbPrinter.TabIndex = 1;

            // panelSave
            this.panelSave.AutoSize = true;
            this.panelSave.Controls.Add(this.btnSave);
            this.panelSave.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelSave.Name = "panelSave";
            this.panelSave.Padding = new System.Windows.Forms.Padding(10);
            this.panelSave.TabIndex = 3;

            // btnSave
            this.btnSave.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.btnSave.Location = new System.Drawing.Point(340, 13);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(150, 30);
            this.btnSave.TabIndex = 0;
            this.btnSave.Text = "Сохранить настройки";
            this.btnSave.UseVisualStyleBackColor = true;

            // NewCasView
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "NewCasView";
            this.Size = new System.Drawing.Size(600, 350);

            this.tableLayoutPanel1.ResumeLayout(false);
            this.panelAddress.ResumeLayout(false);
            this.groupBoxCredentials.ResumeLayout(false);
            this.tableLayoutCredentials.ResumeLayout(false);
            this.groupBoxPrinter.ResumeLayout(false);
            this.tableLayoutPrinter.ResumeLayout(false);
            this.panelSave.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        public System.Windows.Forms.TextBox txtAddress;
        public System.Windows.Forms.TextBox txtRegistrationNumber;
        public System.Windows.Forms.ComboBox cmbPrinter;
        public System.Windows.Forms.Button btnSave;

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Panel panelAddress;
        private System.Windows.Forms.Label labelAddress;
        private System.Windows.Forms.GroupBox groupBoxCredentials;
        private System.Windows.Forms.TableLayoutPanel tableLayoutCredentials;
        private System.Windows.Forms.Label labelRegistrationNumber;
        private System.Windows.Forms.GroupBox groupBoxPrinter;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPrinter;
        private System.Windows.Forms.Label labelPrinter;
        private System.Windows.Forms.Panel panelSave;
    }
}
