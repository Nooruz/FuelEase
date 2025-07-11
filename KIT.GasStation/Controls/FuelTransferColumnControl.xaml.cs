using DevExpress.Mvvm.UI.Interactivity;
using DevExpress.Xpf.Core;
using System.Windows.Controls;

namespace KIT.GasStation.Controls
{
    /// <summary>
    /// Логика взаимодействия для FuelDispenser.xaml
    /// </summary>
    public partial class FuelTransferColumnControl : UserControl
    {
        public FuelTransferColumnControl()
        {
            InitializeComponent();
            Interaction.GetBehaviors(this).Add(new DXMessageBoxService());

            //double minWidth = 280;

            //grid.Width = 10;

            //listBox.SizeChanged += (s, e) =>
            //{
            //    grid.Width = listBox.ActualWidth;
            //};
        }
    }
}
