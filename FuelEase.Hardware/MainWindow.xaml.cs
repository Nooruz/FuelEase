using DevExpress.Mvvm.UI;
using DevExpress.Mvvm.UI.Interactivity;
using DevExpress.Xpf.Core;
using System.Windows;

namespace FuelEase.Hardware
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Interaction.GetBehaviors(this).Add(new WindowService()
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                AllowSetWindowOwner = true,
                Name = "DialogService",
                WindowStyle = new Style
                {
                    TargetType = typeof(ThemedWindow),
                    BasedOn = FindResource("DialogService") as Style
                }
            });
        }
    }
}