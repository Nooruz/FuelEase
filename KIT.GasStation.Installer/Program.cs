using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using WixSharp;
using WixSharp.UI.WPF;

namespace KIT.GasStation.Installer
{
    public class Program
    {
        static void Main(string[] args)
        {
            var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
            var installerPublishRoot = @"D:\\Installer";
            var architecture = (args.FirstOrDefault() ?? "x64").ToLowerInvariant();
            string runtimeIdentifier;
            if (architecture == "x86")
                runtimeIdentifier = "win-x86";
            else if (architecture == "x64" || architecture == "amd64")
                runtimeIdentifier = "win-x64";
            else
                throw new ArgumentException("Неизвестная архитектура. Используйте x86 или x64.");

            var publishDir = Path.Combine(installerPublishRoot, architecture);
            var solutionRoot = Path.Combine(projectRoot, "..");
            var gasStationProject = Path.Combine(solutionRoot, "KIT.GasStation", "KIT.GasStation.csproj");
            var licensePath = Path.Combine(projectRoot, "Assets", "License.rtf");
            var mainExecutable = Path.Combine(publishDir, "KIT.GasStation.exe");

            Directory.CreateDirectory(publishDir);
            PublishApplication(gasStationProject, runtimeIdentifier, publishDir);

            if (!File.Exists(mainExecutable))
                throw new FileNotFoundException("Не найден основной исполняемый файл приложения.", mainExecutable);

            if (!File.Exists(licensePath))
                throw new FileNotFoundException("Не найден файл лицензии для установщика.", licensePath);

            var productVersion = FileVersionInfo.GetVersionInfo(mainExecutable).ProductVersion ?? "1.0.0";
            var parsedVersion = Version.TryParse(productVersion.Split('+')[0], out var version)
                ? version
                : new Version(1, 0, 0, 0);

            var project = new ManagedProject("FuelEase",
                new Dir(@"%ProgramFiles%\\KIT\\FuelEase",
                    new Files(Path.Combine(publishDir, "*.*"))));

            project.GUID = new Guid("9098e750-40c2-4f53-ae3d-fdbb5f9eed2c");
            project.LicenceFile = licensePath;
            project.SourceBaseDir = publishDir;
            project.OutDir = Path.Combine(projectRoot, "artifacts");
            project.OutFileName = "FuelEaseInstaller";
            project.Version = parsedVersion;
            project.MajorUpgrade = new MajorUpgrade
            {
                AllowDowngrades = false,
                DowngradeErrorMessage = "Невозможно установить более старую версию поверх установленной."
            };
            project.ControlPanelInfo.Manufacturer = "ООО \"КИТ\"";
            project.ControlPanelInfo.ProductIcon = Path.Combine(projectRoot, "..", "KIT.GasStation", "logo.ico");

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

            Directory.CreateDirectory(project.OutDir);
            project.BuildMsi();
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
    }
}
