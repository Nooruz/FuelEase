using DevExpress.Xpf.Ribbon;
using DevExpress.Xpf.WindowsUI.Navigation;
using DevExpress.Xpf.WindowsUI;
using FuelEase.Views.Base;
using System.Windows;

namespace FuelEase.Views
{
    /// <summary>
    /// Логика взаимодействия для GlobalReportView.xaml
    /// </summary>
    public partial class GlobalReportView : BaseView
    {
        public GlobalReportView()
        {
            InitializeComponent();
        }

        void OnDocumentFrameNavigating(object sender, NavigatingEventArgs e)
        {
            if (e.Cancel) return;
            NavigationFrame frame = (NavigationFrame)sender;
            FrameworkElement oldContent = (FrameworkElement)frame.Content;
            if (oldContent != null)
            {
                RibbonMergingHelper.SetMergeWith(oldContent, null);
                RibbonMergingHelper.SetMergeStatusBarWith(oldContent, null);
            }
        }
        void OnDocumentFrameNavigated(object sender, DevExpress.Xpf.WindowsUI.Navigation.NavigationEventArgs e)
        {
            FrameworkElement newContent = (FrameworkElement)e.Content;
            if (newContent != null)
            {
                RibbonMergingHelper.SetMergeWith(newContent, ribbon);
                RibbonMergingHelper.SetMergeStatusBarWith(newContent, statusBar);
            }
        }
    }
}
