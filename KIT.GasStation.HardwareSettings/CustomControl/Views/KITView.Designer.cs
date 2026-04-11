namespace KIT.GasStation.HardwareSettings.CustomControl.Views
{
    partial class KITView
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
            this.labelSerialNumber = new System.Windows.Forms.Label();
            this.txtSerialNumber = new System.Windows.Forms.TextBox();
            this.labelSamCardNumber = new System.Windows.Forms.Label();
            this.panelSamCard = new System.Windows.Forms.Panel();
            this.txtSamCardNumber = new System.Windows.Forms.TextBox();
            this.btnTest = new System.Windows.Forms.Button();
            this.panelSave = new System.Windows.Forms.Panel();
            this.btnSave = new System.Windows.Forms.Button();

            this.tableLayoutPanel1.SuspendLayout();
            this.panelAddress.SuspendLayout();
            this.groupBoxCredentials.SuspendLayout();
            this.tableLayoutCredentials.SuspendLayout();
            this.panelSamCard.SuspendLayout();
            this.panelSave.SuspendLayout();
            this.SuspendLayout();

            // tableLayoutPanel1
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.panelAddress, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.groupBoxCredentials, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.panelSave, 0, 2);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 3;
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
            this.labelAddress.Size = new System.Drawing.Size(60, 15);
            this.labelAddress.TabIndex = 0;
            this.labelAddress.Text = "Адрес:";
            this.labelAddress.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // txtAddress
            this.txtAddress.Location = new System.Drawing.Point(100, 3);
            this.txtAddress.Name = "txtAddress";
            this.txtAddress.Size = new System.Drawing.Size(300, 23);
            this.txtAddress.TabIndex = 1;

            // groupBoxCredentials
            this.groupBoxCredentials.AutoSize = true;
            this.groupBoxCredentials.Controls.Add(this.tableLayoutCredentials);
            this.groupBoxCredentials.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBoxCredentials.Name = "groupBoxCredentials";
            this.groupBoxCredentials.Padding = new System.Windows.Forms.Padding(10);
            this.groupBoxCredentials.Size = new System.Drawing.Size(500, 150);
            this.groupBoxCredentials.TabIndex = 1;
            this.groupBoxCredentials.TabStop = false;
            this.groupBoxCredentials.Text = "Реквизиты подключения";

            // tableLayoutCredentials
            this.tableLayoutCredentials.AutoSize = true;
            this.tableLayoutCredentials.ColumnCount = 3;
            this.tableLayoutCredentials.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tableLayoutCredentials.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutCredentials.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tableLayoutCredentials.Controls.Add(this.labelSerialNumber, 0, 0);
            this.tableLayoutCredentials.Controls.Add(this.txtSerialNumber, 1, 0);
            this.tableLayoutCredentials.Controls.Add(this.labelSamCardNumber, 0, 1);
            this.tableLayoutCredentials.Controls.Add(this.panelSamCard, 1, 1);
            this.tableLayoutCredentials.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutCredentials.Name = "tableLayoutCredentials";
            this.tableLayoutCredentials.RowCount = 2;
            this.tableLayoutCredentials.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tableLayoutCredentials.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tableLayoutCredentials.TabIndex = 0;

            // labelSerialNumber
            this.labelSerialNumber.AutoSize = true;
            this.labelSerialNumber.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelSerialNumber.Location = new System.Drawing.Point(3, 3);
            this.labelSerialNumber.Name = "labelSerialNumber";
            this.labelSerialNumber.Size = new System.Drawing.Size(140, 25);
            this.labelSerialNumber.TabIndex = 0;
            this.labelSerialNumber.Text = "Серийный номер ПО:";
            this.labelSerialNumber.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // txtSerialNumber
            this.txtSerialNumber.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtSerialNumber.Location = new System.Drawing.Point(150, 3);
            this.txtSerialNumber.Name = "txtSerialNumber";
            this.txtSerialNumber.Size = new System.Drawing.Size(260, 23);
            this.txtSerialNumber.TabIndex = 1;

            // labelSamCardNumber
            this.labelSamCardNumber.AutoSize = true;
            this.labelSamCardNumber.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelSamCardNumber.Location = new System.Drawing.Point(3, 33);
            this.labelSamCardNumber.Name = "labelSamCardNumber";
            this.labelSamCardNumber.Size = new System.Drawing.Size(140, 30);
            this.labelSamCardNumber.TabIndex = 2;
            this.labelSamCardNumber.Text = "Номер САМ карты:";
            this.labelSamCardNumber.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // panelSamCard
            this.panelSamCard.AutoSize = true;
            this.panelSamCard.Controls.Add(this.txtSamCardNumber);
            this.panelSamCard.Controls.Add(this.btnTest);
            this.panelSamCard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelSamCard.Name = "panelSamCard";
            this.panelSamCard.TabIndex = 3;

            // txtSamCardNumber
            this.txtSamCardNumber.Location = new System.Drawing.Point(0, 3);
            this.txtSamCardNumber.Name = "txtSamCardNumber";
            this.txtSamCardNumber.ReadOnly = true;
            this.txtSamCardNumber.Size = new System.Drawing.Size(260, 23);
            this.txtSamCardNumber.TabIndex = 0;

            // btnTest
            this.btnTest.Location = new System.Drawing.Point(266, 3);
            this.btnTest.Name = "btnTest";
            this.btnTest.Size = new System.Drawing.Size(90, 25);
            this.btnTest.TabIndex = 1;
            this.btnTest.Text = "Проверить";
            this.btnTest.UseVisualStyleBackColor = true;

            // panelSave
            this.panelSave.AutoSize = true;
            this.panelSave.Controls.Add(this.btnSave);
            this.panelSave.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelSave.Name = "panelSave";
            this.panelSave.Padding = new System.Windows.Forms.Padding(10);
            this.panelSave.TabIndex = 2;

            // btnSave
            this.btnSave.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.btnSave.Location = new System.Drawing.Point(340, 13);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(150, 30);
            this.btnSave.TabIndex = 0;
            this.btnSave.Text = "Сохранить настройки";
            this.btnSave.UseVisualStyleBackColor = true;

            // KITView
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "KITView";
            this.Size = new System.Drawing.Size(600, 300);

            this.tableLayoutPanel1.ResumeLayout(false);
            this.panelAddress.ResumeLayout(false);
            this.groupBoxCredentials.ResumeLayout(false);
            this.tableLayoutCredentials.ResumeLayout(false);
            this.panelSamCard.ResumeLayout(false);
            this.panelSave.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        public System.Windows.Forms.TextBox txtAddress;
        public System.Windows.Forms.TextBox txtSerialNumber;
        public System.Windows.Forms.TextBox txtSamCardNumber;
        public System.Windows.Forms.Button btnTest;
        public System.Windows.Forms.Button btnSave;

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Panel panelAddress;
        private System.Windows.Forms.Label labelAddress;
        private System.Windows.Forms.GroupBox groupBoxCredentials;
        private System.Windows.Forms.TableLayoutPanel tableLayoutCredentials;
        private System.Windows.Forms.Label labelSerialNumber;
        private System.Windows.Forms.Label labelSamCardNumber;
        private System.Windows.Forms.Panel panelSamCard;
        private System.Windows.Forms.Panel panelSave;
    }
}
