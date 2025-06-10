using DevExpress.Mvvm.UI;
using DevExpress.Mvvm.UI.Interactivity;
using DevExpress.Xpf.Core;
using System.Windows;
using System.Windows.Controls;

namespace FuelEase.Hardware.Views
{
    public class BaseView : UserControl
    {
        public BaseView()
        {
            Interaction.GetBehaviors(this).Add(new WindowService()
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                AllowSetWindowOwner = true,
                WindowShowMode = WindowShowMode.Dialog,
                Name = "DialogService",
                WindowStyle = new Style
                {
                    TargetType = typeof(ThemedWindow),
                    BasedOn = FindResource("DialogService") as Style
                }
            });
            Interaction.GetBehaviors(this).Add(new DXMessageBoxService());
            Interaction.GetBehaviors(this).Add(new CurrentWindowService());
            Interaction.GetBehaviors(this).Add(new DialogService()
            {
                DialogStyle = new Style
                {
                    TargetType = typeof(Window),
                    BasedOn = FindResource("DialogService") as Style
                }
            });
        }
    }
}
