using DevExpress.Mvvm.UI;
using DevExpress.Mvvm.UI.Interactivity;
using DevExpress.Xpf.Core;
using FuelEase.SplashScreen.ThemedSplashScreen;
using System.Windows;

namespace FuelEase
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : ThemedWindow
    {
        public MainWindow()
        {
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
            InitializeComponent();
        }
    }
}
