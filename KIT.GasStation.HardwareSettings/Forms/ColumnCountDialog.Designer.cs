namespace KIT.GasStation.HardwareSettings.Forms
{
    partial class ColumnCountDialog
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

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            tableLayoutPanel1 = new TableLayoutPanel();
            lblCount = new Label();
            numCount = new NumericUpDown();
            flowButtons = new FlowLayoutPanel();
            btnOk = new Button();
            btnCancel = new Button();
            tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numCount).BeginInit();
            flowButtons.SuspendLayout();
            SuspendLayout();
            //
            // tableLayoutPanel1
            //
            tableLayoutPanel1.ColumnCount = 2;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Controls.Add(lblCount, 0, 0);
            tableLayoutPanel1.Controls.Add(numCount, 1, 0);
            tableLayoutPanel1.Controls.Add(flowButtons, 0, 1);
            tableLayoutPanel1.SetColumnSpan(flowButtons, 2);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.Padding = new Padding(10);
            tableLayoutPanel1.RowCount = 2;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tableLayoutPanel1.Size = new Size(300, 120);
            tableLayoutPanel1.TabIndex = 0;
            //
            // lblCount
            //
            lblCount.Anchor = AnchorStyles.Left;
            lblCount.AutoSize = true;
            lblCount.Location = new Point(13, 15);
            lblCount.Name = "lblCount";
            lblCount.Size = new Size(140, 15);
            lblCount.TabIndex = 0;
            lblCount.Text = "Количество колонок:";
            //
            // numCount
            //
            numCount.Dock = DockStyle.Fill;
            numCount.Location = new Point(159, 13);
            numCount.Name = "numCount";
            numCount.Size = new Size(128, 23);
            numCount.TabIndex = 1;
            //
            // flowButtons
            //
            flowButtons.AutoSize = true;
            flowButtons.Dock = DockStyle.Right;
            flowButtons.FlowDirection = FlowDirection.RightToLeft;
            flowButtons.Controls.Add(btnCancel);
            flowButtons.Controls.Add(btnOk);
            flowButtons.Location = new Point(13, 50);
            flowButtons.Name = "flowButtons";
            //
            // btnOk
            //
            btnOk.Name = "btnOk";
            btnOk.Size = new Size(75, 30);
            btnOk.TabIndex = 2;
            btnOk.Text = "OK";
            btnOk.UseVisualStyleBackColor = true;
            btnOk.Click += btnOk_Click;
            //
            // btnCancel
            //
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(75, 30);
            btnCancel.TabIndex = 3;
            btnCancel.Text = "Отмена";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            //
            // ColumnCountDialog
            //
            AcceptButton = btnOk;
            CancelButton = btnCancel;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(300, 120);
            Controls.Add(tableLayoutPanel1);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ColumnCountDialog";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Добавление колонок";
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numCount).EndInit();
            flowButtons.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel tableLayoutPanel1;
        private Label lblCount;
        private NumericUpDown numCount;
        private FlowLayoutPanel flowButtons;
        private Button btnOk;
        private Button btnCancel;
    }
}
