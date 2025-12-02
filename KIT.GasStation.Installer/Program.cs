using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using WixSharp;

namespace KIT.GasStation.Installer
{
    public class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
                var installerPublishRoot = @"D:\\Installer";
                var architecture = (args.FirstOrDefault() ?? "x64").ToLowerInvariant();
                string runtimeIdentifier = string.Empty;
                if (architecture == "x86")
                    runtimeIdentifier = "win-x86";
                else if (architecture == "x64" || architecture == "amd64")
                    runtimeIdentifier = "win-x64";

                var publishDir = Path.Combine(installerPublishRoot, architecture);
                var solutionRoot = Path.Combine(projectRoot, "..");
                var gasStationProject = Path.Combine(solutionRoot, "KIT.GasStation", "KIT.GasStation.csproj");
                var hardwareProject = Path.Combine(solutionRoot, "KIT.GasStation.Hardware", "KIT.GasStation.Hardware.csproj");
                var webProject = Path.Combine(solutionRoot, "KIT.GasStation.Web", "KIT.GasStation.Web.csproj");
                var workerProject = Path.Combine(solutionRoot, "KIT.GasStation.Worker", "KIT.GasStation.Worker.csproj");
                var licensePath = Path.Combine(projectRoot, "Assets", "License.rtf");
                var mainExecutable = Path.Combine(publishDir, "КИТ-АЗС.exe");

                Directory.CreateDirectory(publishDir);
                PublishApplication(gasStationProject, runtimeIdentifier, publishDir);
                PublishHardwareApplication(hardwareProject, runtimeIdentifier, publishDir);
                PublishWebApplication(webProject, runtimeIdentifier, publishDir);
                PublishWorkerApplication(workerProject, runtimeIdentifier, publishDir);

                //custom set of UI WPF dialogs

                var productVersion = FileVersionInfo.GetVersionInfo(mainExecutable).ProductVersion ?? "1.0.0";
                var parsedVersion = Version.TryParse(productVersion.Split('+')[0], out var version)
                    ? version
                    : new Version(1, 0, 0, 0);

                var mainFeature = new Feature("КИТ-АЗС");
                var hardwareFeature = new Feature("Конфигуратор оборудования");
                var workerFeature = new Feature("Служба");
                var webFeature = new Feature("API");

                var project = new ManagedProject("КИТ-АЗС",
                    new Dir(@"%ProgramFiles%\KIT.GasStation",
                        //new Dir("Hardware", 
                        //    new Files(hardwareFeature, Path.Combine(publishDir, "Hardware"))),
                        //new Dir("Web",
                        //    new WixSharp.File(webFeature, Path.Combine(publishDir, "Web"), 
                        //    new ServiceInstaller
                        //    {
                        //        Name = "KITWeb",
                        //        DisplayName = "КИТ-АЗС Web",
                        //        Description = "Веб API сервис для КИТ-АЗС",
                        //        Start = SvcStartType.auto,
                        //        StartOn = SvcEvent.Install,
                        //        StopOn = SvcEvent.InstallUninstall,
                        //        RemoveOn = SvcEvent.Uninstall,
                        //        DelayedAutoStart = true,
                        //        Account = "LocalSystem"
                        //    }),
                        //    new Files(webFeature, Path.Combine(publishDir, "Web", "*.*"))),
                        //new Dir("Worker",
                        //    new WixSharp.File(workerFeature, Path.Combine(publishDir, "Worker"),
                        //    new ServiceInstaller
                        //    {
                        //        Name = "KITWorker",
                        //        DisplayName = "КИТ-АЗС Worker",
                        //        Description = "Служба для КИТ-АЗС",
                        //        Start = SvcStartType.auto,
                        //        StartOn = SvcEvent.Install,
                        //        StopOn = SvcEvent.InstallUninstall,
                        //        RemoveOn = SvcEvent.Uninstall,
                        //        DelayedAutoStart = true,
                        //        Account = "LocalSystem"
                        //    }),
                        //    new Files(workerFeature, Path.Combine(publishDir, "Worker", "*.*"))),
                        new Files(mainFeature, Path.Combine(publishDir, "*.*"))));

                //project.DefaultFeature = mainFeature;

                project.GUID = new Guid("9098e750-40c2-4f53-ae3d-fdbb5f9eed2c");
                project.LicenceFile = licensePath;
                project.SourceBaseDir = publishDir;
                project.OutDir = Path.Combine(installerPublishRoot, "publish");
                project.OutFileName = "KITGasStationInstaller";
                project.Version = parsedVersion;
                project.MajorUpgrade = new MajorUpgrade
                {
                    AllowDowngrades = false,
                    DowngradeErrorMessage = "Невозможно установить более старую версию поверх установленной."
                };
                project.ControlPanelInfo.Manufacturer = "ОсОО \"КИТ\"";
                project.ControlPanelInfo.ProductIcon = Path.Combine(projectRoot, "..", "KIT.GasStation", "logo.ico");

                project.Language = "ru-RU";
                project.Codepage = "windows-1251";

                project.ManagedUI = new ManagedUI();


                project.ManagedUI.InstallDialogs.Add<WelcomeDialog>()
                                                .Add<LicenceDialog>()
                                                .Add<FeaturesDialog>()
                                                .Add<InstallDirDialog>()
                                                .Add<ProgressDialog>()
                                                .Add<ExitDialog>();

                project.ManagedUI.ModifyDialogs.Add<MaintenanceTypeDialog>()
                                               .Add<FeaturesDialog>()
                                               .Add<ProgressDialog>()
                                               .Add<ExitDialog>();

                //project.SourceBaseDir = "<input dir path>";
                //project.OutDir = "<output dir path>";

                project.BuildMsi();
            }
            catch (Exception ex)
            {
                HandleFailure(ex);
            }
        }

        private static void PublishApplication(string projectPath, string runtime, string outputDirectory)
        {
            var publishArguments = $"publish \"{projectPath}\" -c Release -r {runtime} --self-contained false -o \"{outputDirectory}\"";
            using (var process = Process.Start(new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = publishArguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }))
            {
                if (process == null)
                    throw new InvalidOperationException("Не удалось запустить процесс публикации приложения.");

                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();

                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    var message = string.IsNullOrWhiteSpace(error) ? output : error;
                    throw new InvalidOperationException($"Публикация приложения завершилась с ошибкой ({process.ExitCode}). {message}");
                }
            }
        }

        private static void PublishHardwareApplication(string projectPath, string runtime, string outputDirectory)
        {
            var publishArguments = $"publish \"{projectPath}\" -c Release -r {runtime} " +
                $"/p:PublishSingleFile=true " +
                $"/p:IncludeNativeLibrariesForSelfExtract=true " +
                $"/p:EnableCompressionInSingleFile=true " +
                $"--self-contained false " +
                $"-o \"{Path.Combine(outputDirectory, "Hardware")}\"";

            using (var process = Process.Start(new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = publishArguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }))
            {
                if (process == null)
                    throw new InvalidOperationException("Не удалось запустить процесс публикации приложения Hardware.");

                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();

                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    var message = string.IsNullOrWhiteSpace(error) ? output : error;
                    throw new InvalidOperationException($"Публикация приложения Hardware завершилась с ошибкой ({process.ExitCode}). {message}");
                }
            }
        }

        private static void PublishWebApplication(string projectPath, string runtime, string outputDirectory)
        {
            var publishArguments = $"publish \"{projectPath}\" -c Release -r {runtime} --self-contained false -o \"{Path.Combine(outputDirectory, "Web")}\"";
            using (var process = Process.Start(new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = publishArguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }))
            {
                if (process == null)
                    throw new InvalidOperationException("Не удалось запустить процесс публикации приложения Web.");

                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();

                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    var message = string.IsNullOrWhiteSpace(error) ? output : error;
                    throw new InvalidOperationException($"Публикация приложения Web завершилась с ошибкой ({process.ExitCode}). {message}");
                }
            }
        }

        private static void PublishWorkerApplication(string projectPath, string runtime, string outputDirectory)
        {
            var publishArguments = $"publish \"{projectPath}\" -c Release -r {runtime} --self-contained false -o \"{Path.Combine(outputDirectory, "Worker")}\"";
            using (var process = Process.Start(new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = publishArguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }))
            {
                if (process == null)
                    throw new InvalidOperationException("Не удалось запустить процесс публикации приложения Worker.");

                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();

                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    var message = string.IsNullOrWhiteSpace(error) ? output : error;
                    throw new InvalidOperationException($"Публикация приложения Worker завершилась с ошибкой ({process.ExitCode}). {message}");
                }
            }
        }

        private static void HandleFailure(Exception exception)
        {
            MessageBox.Show(
                "Создание установщика завершилось с ошибкой:\n" + exception.Message,
                "FuelEase Installer",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            Environment.ExitCode = -1;
        }
    }
}