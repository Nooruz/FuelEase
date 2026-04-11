namespace KIT.GasStation.HardwareSettings
{
    partial class Main
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
            components = new System.ComponentModel.Container();
            // Context menus
            cmsControllerParent = new ContextMenuStrip(components);
            tsmiAddFuelDispenser = new ToolStripMenuItem();
            cmsControllerItem = new ContextMenuStrip(components);
            tsmiDeleteController = new ToolStripMenuItem();
            cmsCashRegisterParent = new ContextMenuStrip(components);
            tsmiAddCashRegister = new ToolStripMenuItem();
            cmsCashRegisterItem = new ContextMenuStrip(components);
            tsmiDeleteCashRegister = new ToolStripMenuItem();
            // Layout
            tableLayoutPanel1 = new TableLayoutPanel();
            menuStrip1 = new MenuStrip();
            файлToolStripMenuItem = new ToolStripMenuItem();
            выходToolStripMenuItem = new ToolStripMenuItem();
            справкаToolStripMenuItem = new ToolStripMenuItem();
            опрограммеToolStripMenuItem = new ToolStripMenuItem();
            treeView1 = new TreeView();
            panelContent = new Panel();
            nodeControllers = new TreeNode("Устройства ТРК");
            nodeCashRegisters = new TreeNode("ККМ");

            cmsControllerParent.SuspendLayout();
            cmsControllerItem.SuspendLayout();
            cmsCashRegisterParent.SuspendLayout();
            cmsCashRegisterItem.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            menuStrip1.SuspendLayout();
            SuspendLayout();
            //
            // cmsControllerParent
            //
            cmsControllerParent.Items.AddRange(new ToolStripItem[] { tsmiAddFuelDispenser });
            cmsControllerParent.Name = "cmsControllerParent";
            cmsControllerParent.Size = new Size(140, 26);
            //
            // tsmiAddFuelDispenser
            //
            tsmiAddFuelDispenser.Name = "tsmiAddFuelDispenser";
            tsmiAddFuelDispenser.Size = new Size(139, 22);
            tsmiAddFuelDispenser.Text = "Добавить";
            tsmiAddFuelDispenser.Click += tsmiAddFuelDispenser_Click;
            //
            // cmsControllerItem
            //
            cmsControllerItem.Items.AddRange(new ToolStripItem[] { tsmiDeleteController });
            cmsControllerItem.Name = "cmsControllerItem";
            cmsControllerItem.Size = new Size(127, 26);
            //
            // tsmiDeleteController
            //
            tsmiDeleteController.Name = "tsmiDeleteController";
            tsmiDeleteController.Size = new Size(126, 22);
            tsmiDeleteController.Text = "Удалить";
            tsmiDeleteController.Click += tsmiDeleteController_Click;
            //
            // cmsCashRegisterParent
            //
            cmsCashRegisterParent.Items.AddRange(new ToolStripItem[] { tsmiAddCashRegister });
            cmsCashRegisterParent.Name = "cmsCashRegisterParent";
            cmsCashRegisterParent.Size = new Size(140, 26);
            //
            // tsmiAddCashRegister
            //
            tsmiAddCashRegister.Name = "tsmiAddCashRegister";
            tsmiAddCashRegister.Size = new Size(139, 22);
            tsmiAddCashRegister.Text = "Добавить";
            tsmiAddCashRegister.Click += tsmiAddCashRegister_Click;
            //
            // cmsCashRegisterItem
            //
            cmsCashRegisterItem.Items.AddRange(new ToolStripItem[] { tsmiDeleteCashRegister });
            cmsCashRegisterItem.Name = "cmsCashRegisterItem";
            cmsCashRegisterItem.Size = new Size(127, 26);
            //
            // tsmiDeleteCashRegister
            //
            tsmiDeleteCashRegister.Name = "tsmiDeleteCashRegister";
            tsmiDeleteCashRegister.Size = new Size(126, 22);
            tsmiDeleteCashRegister.Text = "Удалить";
            tsmiDeleteCashRegister.Click += tsmiDeleteCashRegister_Click;
            //
            // tableLayoutPanel1
            //
            tableLayoutPanel1.ColumnCount = 2;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Controls.Add(menuStrip1, 0, 0);
            tableLayoutPanel1.Controls.Add(treeView1, 0, 1);
            tableLayoutPanel1.Controls.Add(panelContent, 1, 1);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 2;
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Size = new Size(1136, 726);
            tableLayoutPanel1.TabIndex = 0;
            //
            // menuStrip1
            //
            tableLayoutPanel1.SetColumnSpan(menuStrip1, 2);
            menuStrip1.Items.AddRange(new ToolStripItem[] { файлToolStripMenuItem, справкаToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(1136, 24);
            menuStrip1.TabIndex = 0;
            //
            // файлToolStripMenuItem
            //
            файлToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { выходToolStripMenuItem });
            файлToolStripMenuItem.Name = "файлToolStripMenuItem";
            файлToolStripMenuItem.Size = new Size(48, 20);
            файлToolStripMenuItem.Text = "&Файл";
            //
            // выходToolStripMenuItem
            //
            выходToolStripMenuItem.Name = "выходToolStripMenuItem";
            выходToolStripMenuItem.Size = new Size(233, 22);
            выходToolStripMenuItem.Text = "Вы&ход";
            //
            // справкаToolStripMenuItem
            //
            справкаToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { опрограммеToolStripMenuItem });
            справкаToolStripMenuItem.Name = "справкаToolStripMenuItem";
            справкаToolStripMenuItem.Size = new Size(65, 20);
            справкаToolStripMenuItem.Text = "&Справка";
            //
            // опрограммеToolStripMenuItem
            //
            опрограммеToolStripMenuItem.Name = "опрограммеToolStripMenuItem";
            опрограммеToolStripMenuItem.Size = new Size(158, 22);
            опрограммеToolStripMenuItem.Text = "&О программе…";
            //
            // treeView1
            //
            treeView1.Dock = DockStyle.Fill;
            treeView1.Location = new Point(3, 27);
            treeView1.Name = "treeView1";
            nodeControllers.ContextMenuStrip = cmsControllerParent;
            nodeControllers.Name = "nodeControllers";
            nodeCashRegisters.ContextMenuStrip = cmsCashRegisterParent;
            nodeCashRegisters.Name = "nodeCashRegisters";
            treeView1.Nodes.AddRange(new TreeNode[] { nodeControllers, nodeCashRegisters });
            treeView1.Size = new Size(294, 696);
            treeView1.TabIndex = 1;
            treeView1.AfterSelect += treeView1_AfterSelect;
            treeView1.MouseUp += treeView1_MouseUp;
            //
            // panelContent
            //
            panelContent.Dock = DockStyle.Fill;
            panelContent.Location = new Point(303, 27);
            panelContent.Name = "panelContent";
            panelContent.Size = new Size(830, 696);
            panelContent.TabIndex = 2;
            //
            // Main
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1136, 726);
            Controls.Add(tableLayoutPanel1);
            MainMenuStrip = menuStrip1;
            Name = "Main";
            Text = "КИТ-АЗС Конфигуратор";
            WindowState = FormWindowState.Maximized;
            cmsControllerParent.ResumeLayout(false);
            cmsControllerItem.ResumeLayout(false);
            cmsCashRegisterParent.ResumeLayout(false);
            cmsCashRegisterItem.ResumeLayout(false);
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel tableLayoutPanel1;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem файлToolStripMenuItem;
        private ToolStripMenuItem выходToolStripMenuItem;
        private ToolStripMenuItem справкаToolStripMenuItem;
        private ToolStripMenuItem опрограммеToolStripMenuItem;
        private TreeView treeView1;
        private Panel panelContent;
        private ContextMenuStrip cmsControllerParent;
        private ToolStripMenuItem tsmiAddFuelDispenser;
        private ContextMenuStrip cmsControllerItem;
        private ToolStripMenuItem tsmiDeleteController;
        private ContextMenuStrip cmsCashRegisterParent;
        private ToolStripMenuItem tsmiAddCashRegister;
        private ContextMenuStrip cmsCashRegisterItem;
        private ToolStripMenuItem tsmiDeleteCashRegister;
        private TreeNode nodeControllers;
        private TreeNode nodeCashRegisters;
    }
}
