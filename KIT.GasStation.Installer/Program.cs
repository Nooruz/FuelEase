using System;
using System.Windows.Forms;
using WixSharp;
using WixSharp.UI.WPF;

namespace KIT.GasStation.Installer
{
    public class Program
    {
        static void Main()
        {
            var project = new ManagedProject("MyProduct",
                              new Dir(@"%ProgramFiles%\My Company\My Product",
                                  new File("Program.cs")));

            project.GUID = new Guid("9098e750-40c2-4f53-ae3d-fdbb5f9eed2c");

            // project.ManagedUI = ManagedUI.DefaultWpf; // all stock UI dialogs

            //custom set of UI WPF dialogs
            project.ManagedUI = new ManagedUI();

            project.ManagedUI.InstallDialogs.Add<KIT.GasStation.Installer.WelcomeDialog>()
                                            .Add<KIT.GasStation.Installer.LicenceDialog>()
                                            .Add<KIT.GasStation.Installer.FeaturesDialog>()
                                            .Add<KIT.GasStation.Installer.InstallDirDialog>()
                                            .Add<KIT.GasStation.Installer.ProgressDialog>()
                                            .Add<KIT.GasStation.Installer.ExitDialog>();

            project.ManagedUI.ModifyDialogs.Add<KIT.GasStation.Installer.MaintenanceTypeDialog>()
                                           .Add<KIT.GasStation.Installer.FeaturesDialog>()
                                           .Add<KIT.GasStation.Installer.ProgressDialog>()
                                           .Add<KIT.GasStation.Installer.ExitDialog>();

            //project.SourceBaseDir = "<input dir path>";
            //project.OutDir = "<output dir path>";

            project.BuildMsi();
        }
    }
}