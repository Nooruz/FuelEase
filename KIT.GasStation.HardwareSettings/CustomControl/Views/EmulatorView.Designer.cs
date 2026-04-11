namespace KIT.GasStation.HardwareSettings.CustomControl.Views
{
    partial class EmulatorView
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
            tableLayoutPanel1 = new TableLayoutPanel();
            dgvColumns = new DataGridView();
            panelButtons = new Panel();
            btnSave = new Button();
            btnDeleteColumn = new Button();
            btnAddColumn = new Button();
            tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvColumns).BeginInit();
            panelButtons.SuspendLayout();
            SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Controls.Add(dgvColumns, 0, 0);
            tableLayoutPanel1.Controls.Add(panelButtons, 0, 1);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 2;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.Size = new Size(740, 400);
            tableLayoutPanel1.TabIndex = 0;
            // 
            // dgvColumns
            // 
            dgvColumns.AllowUserToAddRows = false;
            dgvColumns.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvColumns.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvColumns.Dock = DockStyle.Fill;
            dgvColumns.Location = new Point(3, 3);
            dgvColumns.Name = "dgvColumns";
            dgvColumns.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvColumns.Size = new Size(734, 332);
            dgvColumns.TabIndex = 0;
            // 
            // panelButtons
            // 
            panelButtons.AutoSize = true;
            panelButtons.Controls.Add(btnSave);
            panelButtons.Controls.Add(btnDeleteColumn);
            panelButtons.Controls.Add(btnAddColumn);
            panelButtons.Dock = DockStyle.Fill;
            panelButtons.Location = new Point(3, 341);
            panelButtons.Name = "panelButtons";
            panelButtons.Padding = new Padding(10);
            panelButtons.Size = new Size(734, 56);
            panelButtons.TabIndex = 1;
            // 
            // btnSave
            // 
            btnSave.Anchor = AnchorStyles.Right;
            btnSave.Location = new Point(981, -9);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(150, 30);
            btnSave.TabIndex = 2;
            btnSave.Text = "Сохранить настройки";
            btnSave.UseVisualStyleBackColor = true;
            // 
            // btnDeleteColumn
            // 
            btnDeleteColumn.Location = new Point(169, 13);
            btnDeleteColumn.Name = "btnDeleteColumn";
            btnDeleteColumn.Size = new Size(150, 30);
            btnDeleteColumn.TabIndex = 1;
            btnDeleteColumn.Text = "Удалить колонку";
            btnDeleteColumn.UseVisualStyleBackColor = true;
            // 
            // btnAddColumn
            // 
            btnAddColumn.Location = new Point(13, 13);
            btnAddColumn.Name = "btnAddColumn";
            btnAddColumn.Size = new Size(150, 30);
            btnAddColumn.TabIndex = 0;
            btnAddColumn.Text = "Добавить колонку";
            btnAddColumn.UseVisualStyleBackColor = true;
            // 
            // EmulatorView
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(tableLayoutPanel1);
            Name = "EmulatorView";
            Size = new Size(740, 400);
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvColumns).EndInit();
            panelButtons.ResumeLayout(false);
            ResumeLayout(false);
        }

        public System.Windows.Forms.DataGridView dgvColumns;
        public System.Windows.Forms.Button btnAddColumn;
        public System.Windows.Forms.Button btnDeleteColumn;
        public System.Windows.Forms.Button btnSave;

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Panel panelButtons;
    }
}
