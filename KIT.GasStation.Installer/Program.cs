using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using WixSharp;
using IoFile = System.IO.File;
using WixFile = WixSharp.File;

namespace KIT.GasStation.Installer
{
    public class Program
    {
        // ------- Пути -------
        public const string PublishRoot = @"D:\Installer\Publish\KIT GasStation";
        public const string InstallerOutDir = @"D:\Installer\Publish";

        // Папки проектов в solution (источники)
        private const string MainProjectSource = "KIT.GasStation";
        private const string HardwareProjectSource = "KIT.GasStation.Hardware";
        private const string WebProjectSource = "KIT.GasStation.Web";
        private const string WorkerProjectSource = "KIT.GasStation.Worker";

        // Папки публикации и установки (новые имена без префикса KIT.GasStation)
        public const string MainFolder = "КИТ-АЗС";
        public const string HardwareFolder = "Hardware";
        public const string WebFolder = "Web";
        public const string WorkerFolder = "Worker";

        // Имена Windows-служб (совпадают с appsettings.json основного приложения)
        public const string WebServiceName = "KITWeb";
        public const string WorkerServiceName = "KITWorker";

        // Имена исполняемых файлов служб (берутся из AssemblyName соответствующих csproj)
        public const string WebServiceExe = "КИТ-АЗС Служба ТРК.exe";
        public const string WorkerServiceExe = "КИТ-АЗС Сервер обмена.exe";

        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
                var solutionRoot = Path.GetFullPath(Path.Combine(projectRoot, ".."));
                var architecture = (args.FirstOrDefault() ?? "x64").ToLowerInvariant();
                string runtimeIdentifier;
                if (architecture == "x86")
                    runtimeIdentifier = "win-x86";
                else if (architecture == "x64" || architecture == "amd64")
                    runtimeIdentifier = "win-x64";
                else
                    throw new ArgumentException($"Неподдерживаемая архитектура: {architecture}");

                var gasStationProject = Path.Combine(solutionRoot, MainProjectSource, "KIT.GasStation.csproj");
                var hardwareProject = Path.Combine(solutionRoot, HardwareProjectSource, "KIT.GasStation.Hardware.csproj");
                var webProject = Path.Combine(solutionRoot, WebProjectSource, "KIT.GasStation.Web.csproj");
                var workerProject = Path.Combine(solutionRoot, WorkerProjectSource, "KIT.GasStation.Worker.csproj");

                var licensePath = Path.Combine(projectRoot, "Assets", "License.rtf");
                var iconPath = Path.Combine(solutionRoot, MainProjectSource, "logo.ico");

                var mainPublishDir = Path.Combine(PublishRoot, MainFolder);
                var hardwarePublishDir = Path.Combine(PublishRoot, HardwareFolder);
                var webPublishDir = Path.Combine(PublishRoot, WebFolder);
                var workerPublishDir = Path.Combine(PublishRoot, WorkerFolder);

                Directory.CreateDirectory(PublishRoot);
                Directory.CreateDirectory(InstallerOutDir);

                PublishProject(gasStationProject, runtimeIdentifier, mainPublishDir);
                PublishProject(hardwareProject, runtimeIdentifier, hardwarePublishDir,
                    new Dictionary<string, string>
                    {
                        ["PublishSingleFile"] = "true",
                        ["IncludeNativeLibrariesForSelfExtract"] = "true",
                        ["EnableCompressionInSingleFile"] = "true"
                    });
                PublishProject(webProject, runtimeIdentifier, webPublishDir);
                PublishProject(workerProject, runtimeIdentifier, workerPublishDir);

                var mainExecutable = Path.Combine(mainPublishDir, "КИТ-АЗС.exe");
                if (!IoFile.Exists(mainExecutable))
                    throw new FileNotFoundException($"Главный исполняемый файл не найден: {mainExecutable}");

                var productVersion = FileVersionInfo.GetVersionInfo(mainExecutable).ProductVersion ?? "1.0.0";
                var parsedVersion = Version.TryParse(productVersion.Split('+')[0], out var version)
                    ? version
                    : new Version(1, 0, 0, 0);

                // Фичи
                var mainFeature = new Feature("КИТ-АЗС", "Основное приложение КИТ-АЗС", true, false);
                var hardwareFeature = new Feature("Конфигуратор оборудования",
                    "Приложение для настройки оборудования АЗС", true, true);
                var webFeature = new Feature("API (Сервер обмена)",
                    "Веб-сервис обмена данными", true, true);
                var workerFeature = new Feature("Служба ТРК",
                    "Фоновая служба управления ТРК", true, true);

                var project = new ManagedProject("КИТ-АЗС",
                    new Dir(@"%ProgramFiles%\КИТ-АЗС",
                        new Dir(mainFeature, MainFolder,
                            new Files(mainFeature, Path.Combine(mainPublishDir, "*.*"))),
                        new Dir(hardwareFeature, HardwareFolder,
                            new Files(hardwareFeature, Path.Combine(hardwarePublishDir, "*.*"))),
                        new Dir(webFeature, WebFolder,
                            new WixFile(webFeature, Path.Combine(webPublishDir, WebServiceExe),
                                new ServiceInstaller
                                {
                                    Name = WebServiceName,
                                    DisplayName = "КИТ-АЗС Web API",
                                    Description = "Веб API сервис для КИТ-АЗС",
                                    // Запуск вручную, никакого автозапуска после установки
                                    Start = SvcStartType.demand,
                                    // Не стартуем при установке — основной проект поднимает службы сам
                                    StopOn = SvcEvent.InstallUninstall,
                                    RemoveOn = SvcEvent.Uninstall,
                                    Account = "LocalSystem",
                                    Vital = true
                                }),
                            new Files(webFeature, Path.Combine(webPublishDir, "*.*"),
                                f => !Path.GetFileName(f).Equals(WebServiceExe, StringComparison.OrdinalIgnoreCase))),
                        new Dir(workerFeature, WorkerFolder,
                            new WixFile(workerFeature, Path.Combine(workerPublishDir, WorkerServiceExe),
                                new ServiceInstaller
                                {
                                    Name = WorkerServiceName,
                                    DisplayName = "КИТ-АЗС Worker",
                                    Description = "Фоновая служба КИТ-АЗС",
                                    Start = SvcStartType.demand,
                                    StopOn = SvcEvent.InstallUninstall,
                                    RemoveOn = SvcEvent.Uninstall,
                                    Account = "LocalSystem",
                                    Vital = true
                                }),
                            new Files(workerFeature, Path.Combine(workerPublishDir, "*.*"),
                                f => !Path.GetFileName(f).Equals(WorkerServiceExe, StringComparison.OrdinalIgnoreCase)))));

                project.GUID = new Guid("9098e750-40c2-4f53-ae3d-fdbb5f9eed2c");
                project.LicenceFile = licensePath;
                project.SourceBaseDir = PublishRoot;
                project.OutDir = InstallerOutDir;
                project.OutFileName = $"KITGasStationInstaller.{architecture}";
                project.Version = parsedVersion;
                project.MajorUpgrade = new MajorUpgrade
                {
                    AllowDowngrades = false,
                    DowngradeErrorMessage = "Невозможно установить более старую версию поверх установленной."
                };
                project.ControlPanelInfo.Manufacturer = "ОсОО \"КИТ\"";
                if (IoFile.Exists(iconPath))
                    project.ControlPanelInfo.ProductIcon = iconPath;

                project.Platform = architecture == "x86" ? Platform.x86 : Platform.x64;
                project.Language = "ru-RU";
                project.Codepage = "windows-1251";

                // Свойства, которые пробрасываются UI → Deferred Custom Actions
                project.Properties = new[]
                {
                    new Property("LICENSE_KEY", ""),
                    new Property("DB_SERVER", "."),
                    new Property("DB_NAME", "FuelEaseDb"),
                    new Property("DB_APP_LOGIN", "KITGasStationApp"),
                    new Property("DB_APP_PASSWORD", ""),
                    new Property("SERVICE_NAMES", WebServiceName + ";" + WorkerServiceName)
                };

                // Custom actions:
                //   1) InitializeDatabase — создаёт SQL-логин и шифрованный файл credentials.dat
                //   2) GrantServicePermissions — даёт локальным пользователям право Start/Stop
                //   3) WriteActivation — сохраняет ключ активации в защищённый файл
                project.Actions = new WixSharp.Action[]
                {
                    new ElevatedManagedAction(CustomActions.InitializeDatabase,
                        Return.check, When.After, Step.InstallFiles, Condition.NOT_Installed)
                    {
                        UsesProperties = "DB_SERVER,DB_NAME,DB_APP_LOGIN,DB_APP_PASSWORD,INSTALLDIR"
                    },
                    new ElevatedManagedAction(CustomActions.WriteActivation,
                        Return.ignore, When.After, Step.InstallFiles, Condition.NOT_Installed)
                    {
                        UsesProperties = "LICENSE_KEY,INSTALLDIR"
                    },
                    new ElevatedManagedAction(CustomActions.GrantServicePermissions,
                        Return.ignore, When.After, Step.InstallServices, Condition.NOT_Installed)
                    {
                        UsesProperties = "SERVICE_NAMES"
                    }
                };

                project.ManagedUI = new ManagedUI();
                project.ManagedUI.InstallDialogs
                    .Add<WelcomeDialog>()
                    .Add<LicenceDialog>()
                    .Add<ActivationDialog>()
                    .Add<FeaturesDialog>()
                    .Add<InstallDirDialog>()
                    .Add<ProgressDialog>()
                    .Add<ExitDialog>();

                project.ManagedUI.ModifyDialogs
                    .Add<MaintenanceTypeDialog>()
                    .Add<FeaturesDialog>()
                    .Add<ProgressDialog>()
                    .Add<ExitDialog>();

                project.BuildMsi();
            }
            catch (Exception ex)
            {
                HandleFailure(ex);
            }
        }

        /// <summary>
        /// Универсальная публикация .NET-проекта в указанную папку через dotnet publish.
        /// </summary>
        private static void PublishProject(
            string projectPath,
            string runtime,
            string outputDirectory,
            IDictionary<string, string> extraProperties = null)
        {
            if (!IoFile.Exists(projectPath))
                throw new FileNotFoundException($"Проект не найден: {projectPath}");

            Directory.CreateDirectory(outputDirectory);

            var arguments = $"publish \"{projectPath}\" -c Release -r {runtime} --self-contained false -o \"{outputDirectory}\"";
            if (extraProperties != null)
            {
                foreach (var kvp in extraProperties)
                    arguments += $" /p:{kvp.Key}={kvp.Value}";
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            {
                if (process == null)
                    throw new InvalidOperationException($"Не удалось запустить процесс публикации для {Path.GetFileName(projectPath)}.");

                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();

                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    var message = string.IsNullOrWhiteSpace(error) ? output : error;
                    throw new InvalidOperationException(
                        $"Публикация {Path.GetFileNameWithoutExtension(projectPath)} завершилась с ошибкой ({process.ExitCode}). {message}");
                }
            }
        }

        private static void HandleFailure(Exception exception)
        {
            MessageBox.Show(
                "Создание установщика завершилось с ошибкой:\n" + exception.Message,
                "KIT.GasStation Installer",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            Environment.ExitCode = -1;
        }
    }
}
